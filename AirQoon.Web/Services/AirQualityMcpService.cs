using AirQoon.Web.Models.Dtos;
using System.Text.RegularExpressions;

namespace AirQoon.Web.Services;

public class AirQualityMcpService : IAirQualityMcpService
{
    private readonly IMcpClientService _mcp;

    public AirQualityMcpService(IMcpClientService mcp)
    {
        _mcp = mcp;
    }

    public async Task<TimeRangeAnalysisResult> TenantTimeRangeAnalysisAsync(
        string tenantSlug,
        DateTime startDate,
        DateTime endDate,
        List<string>? pollutants = null,
        DateTime? comparisonStartDate = null,
        DateTime? comparisonEndDate = null,
        CancellationToken cancellationToken = default)
    {
        var raw = await _mcp.CallToolAsync(
            "tenant_time_range_analysis",
            new
            {
                tenant_slug = tenantSlug,
                start_date = startDate.ToString("yyyy-MM-dd"),
                end_date = endDate.ToString("yyyy-MM-dd"),
                comparison_start_date = comparisonStartDate?.ToString("yyyy-MM-dd"),
                comparison_end_date = comparisonEndDate?.ToString("yyyy-MM-dd"),
                pollutants = pollutants
            },
            cancellationToken);

        return new TimeRangeAnalysisResult
        {
            TenantSlug = tenantSlug,
            StartDate = startDate,
            EndDate = endDate,
            ComparisonStartDate = comparisonStartDate,
            ComparisonEndDate = comparisonEndDate,
            RawText = raw
        };
    }

    public async Task<MonthlyComparisonResult> TenantMonthlyComparisonAsync(
        string tenantSlug,
        string month1,
        string month2,
        int? year = null,
        CancellationToken cancellationToken = default)
    {
        var raw = await _mcp.CallToolAsync(
            "tenant_monthly_comparison",
            new
            {
                tenant_slug = tenantSlug,
                month1,
                month2,
                year
            },
            cancellationToken);

        return new MonthlyComparisonResult
        {
            TenantSlug = tenantSlug,
            Month1 = month1,
            Month2 = month2,
            RawText = raw
        };
    }

    public async Task<IReadOnlyList<DeviceInfo>> GetTenantDevicesAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        var raw = await _mcp.CallToolAsync(
            "tenant_device_list",
            new { tenant_slug = tenantSlug },
            cancellationToken);

        return new List<DeviceInfo>
        {
            new()
            {
                DeviceId = null,
                Name = null,
                Label = raw
            }
        };
    }

    public async Task<TenantStatistics> GetTenantStatisticsAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        var raw = await _mcp.CallToolAsync(
            "tenant_statistics",
            new { tenant_slug = tenantSlug },
            cancellationToken);

        var result = new TenantStatistics
        {
            TenantSlug = tenantSlug,
            RawText = raw
        };

        // Example:
        // # Akçansa ... - İstatistikler
        // **Cihaz Sayısı:** 18
        // **Vector DB Points:** 5
        // **Public:** Hayır
        var titleMatch = Regex.Match(raw ?? string.Empty, "^#\\s+(?<name>.+?)\\s+-\\s+İstatistikler\\s*$", RegexOptions.Multiline);
        if (titleMatch.Success)
        {
            result.TenantName = titleMatch.Groups["name"].Value.Trim();
        }

        var deviceMatch = Regex.Match(raw ?? string.Empty, "\\*\\*Cihaz Sayısı:\\*\\*\\s+(?<n>\\d+)");
        if (deviceMatch.Success && int.TryParse(deviceMatch.Groups["n"].Value, out var dc))
        {
            result.DeviceCount = dc;
        }

        var vectorMatch = Regex.Match(raw ?? string.Empty, "\\*\\*Vector DB Points:\\*\\*\\s+(?<n>\\d+)");
        if (vectorMatch.Success && long.TryParse(vectorMatch.Groups["n"].Value, out var vp))
        {
            result.VectorPoints = vp;
        }

        var publicMatch = Regex.Match(raw ?? string.Empty, "\\*\\*Public:\\*\\*\\s+(?<v>.+)");
        if (publicMatch.Success)
        {
            var v = publicMatch.Groups["v"].Value.Trim();
            result.IsPublic = string.Equals(v, "Evet", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(v, "Yes", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(v, "True", StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    public Task<string> SaveAnalysisToVectorDbAsync(
        string tenantSlug,
        string analysisText,
        string analysisType = "analysis",
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return _mcp.CallToolAsync(
            "save_analysis_to_vector_db",
            new
            {
                tenant_slug = tenantSlug,
                analysis_text = analysisText,
                analysis_type = analysisType,
                metadata = metadata ?? new Dictionary<string, object>()
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<AnalysisSearchResult>> SearchAnalysisFromVectorDbAsync(
        string tenantSlug,
        string queryText,
        int limit = 5,
        double scoreThreshold = 0.5,
        string? filterType = null,
        CancellationToken cancellationToken = default)
    {
        // The python MCP already returns a formatted text; expose it as a single result.
        return SearchFallbackAsync(tenantSlug, queryText, limit, scoreThreshold, filterType, cancellationToken);
    }

    private async Task<IReadOnlyList<AnalysisSearchResult>> SearchFallbackAsync(
        string tenantSlug,
        string queryText,
        int limit,
        double scoreThreshold,
        string? filterType,
        CancellationToken cancellationToken)
    {
        var raw = await _mcp.CallToolAsync(
            "search_analysis_from_vector_db",
            new
            {
                tenant_slug = tenantSlug,
                query_text = queryText,
                limit,
                score_threshold = scoreThreshold,
                filter_type = filterType
            },
            cancellationToken);

        return new List<AnalysisSearchResult>
        {
            new()
            {
                Score = 1.0,
                Text = raw,
                AnalysisType = filterType,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}
