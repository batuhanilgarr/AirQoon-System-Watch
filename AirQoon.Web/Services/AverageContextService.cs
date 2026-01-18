using System.Text;

namespace AirQoon.Web.Services;

/// <summary>
/// Builds context from telemetry averages to enrich prompts.
/// Automatically adds last 30 days averages to system prompts for MCP.
/// </summary>
public class AverageContextService : IAverageContextService
{
    private readonly IPostgresAirQualityService _airQuality;
    private readonly IMongoDbService _mongo;
    private readonly ILogger<AverageContextService> _logger;

    public AverageContextService(
        IPostgresAirQualityService airQuality,
        IMongoDbService mongo,
        ILogger<AverageContextService> logger)
    {
        _airQuality = airQuality;
        _mongo = mongo;
        _logger = logger;
    }

    public async Task<string> BuildAveragesContextAsync(
        string tenantSlug,
        int daysBack = 30,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summaries = await GetPollutantSummariesAsync(tenantSlug, daysBack, avgType, cancellationToken);
            
            if (summaries.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"## Son {daysBack} Günlük Ortalamalar ({tenantSlug})");
            sb.AppendLine();
            sb.AppendLine($"**Ortalama Tipi:** {avgType}");
            sb.AppendLine();

            foreach (var (pollutant, stats) in summaries.OrderBy(x => x.Key))
            {
                if (!stats.TryGetValue("average", out var avg) || !avg.HasValue)
                {
                    continue;
                }

                if (double.IsNaN(avg.Value) || string.Equals(avg.Value.ToString(), "NaN", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var min = stats.TryGetValue("minimum", out var minVal) ? minVal : null;
                var max = stats.TryGetValue("maximum", out var maxVal) ? maxVal : null;
                var count = stats.TryGetValue("count", out var countVal) ? countVal : null;

                if (min.HasValue && (double.IsNaN(min.Value) || string.Equals(min.Value.ToString(), "NaN", StringComparison.OrdinalIgnoreCase))) min = null;
                if (max.HasValue && (double.IsNaN(max.Value) || string.Equals(max.Value.ToString(), "NaN", StringComparison.OrdinalIgnoreCase))) max = null;
                if (count.HasValue && (double.IsNaN(count.Value) || string.Equals(count.Value.ToString(), "NaN", StringComparison.OrdinalIgnoreCase))) count = null;

                sb.AppendLine($"### {pollutant}");
                sb.AppendLine($"- Ortalama: {avg:F2} µg/m³");
                
                if (min.HasValue)
                {
                    sb.AppendLine($"- Minimum: {min:F2} µg/m³");
                }
                
                if (max.HasValue)
                {
                    sb.AppendLine($"- Maksimum: {max:F2} µg/m³");
                }
                
                if (count.HasValue)
                {
                    sb.AppendLine($"- Ölçüm sayısı: {count:F0}");
                }
                
                sb.AppendLine();
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build averages context for tenant: {TenantSlug}", tenantSlug);
            return string.Empty;
        }
    }

    public async Task<Dictionary<string, Dictionary<string, double?>>> GetPollutantSummariesAsync(
        string tenantSlug,
        int daysBack = 30,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default)
    {
        var summaries = new Dictionary<string, Dictionary<string, double?>>();

        try
        {
            // Get tenant devices
            var devices = await _mongo.GetDevicesByTenantSlugAsync(tenantSlug, 500, cancellationToken);
            var deviceIds = devices.Select(d => d.DeviceId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            if (deviceIds.Count == 0)
            {
                _logger.LogWarning("No devices found for tenant: {TenantSlug}", tenantSlug);
                return summaries;
            }

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-daysBack);

            // Get statistics for each pollutant
            var pollutants = new[] { "PM10", "PM2.5", "NO2", "O3", "SO2", "CO" };

            foreach (var pollutant in pollutants)
            {
                try
                {
                    var stats = await _airQuality.GetTelemetryAverageStatsAsync(
                        deviceIds,
                        startDate,
                        endDate,
                        pollutant,
                        avgType,
                        cancellationToken);

                    if (stats.Count > 0 && stats.TryGetValue("average", out var avg) && avg.HasValue)
                    {
                        summaries[pollutant] = stats;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get stats for pollutant {Pollutant} in tenant {TenantSlug}", 
                        pollutant, tenantSlug);
                }
            }

            return summaries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pollutant summaries for tenant: {TenantSlug}", tenantSlug);
            return summaries;
        }
    }
}
