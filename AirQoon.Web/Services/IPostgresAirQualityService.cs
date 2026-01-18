using AirQoon.Web.Models.Dtos;

namespace AirQoon.Web.Services;

public interface IPostgresAirQualityService
{
    IReadOnlyList<string> NormalizePollutants(IEnumerable<string> pollutants);

    Task<IReadOnlyList<AirQualityAggregate>> GetAggregatesAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        IReadOnlyList<string> pollutants,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get telemetry averages from telemetry_averages table (WIDE FORMAT).
    /// This table contains pre-calculated averages - do NOT calculate at runtime.
    /// </summary>
    Task<IReadOnlyList<TelemetryAverage>> GetTelemetryAveragesAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string avgType = "24h_rolling", // '1h', '24h_rolling', '8h_rolling'
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get aggregated statistics from telemetry_averages for a specific pollutant.
    /// </summary>
    Task<Dictionary<string, double?>> GetTelemetryAverageStatsAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        string pollutant, // "PM10", "PM25", "NO2", etc.
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetLatestTelemetryAverageTimestampAsync(
        IReadOnlyList<string> deviceIds,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, DateTime>> GetLatestTelemetryAverageTimestampsAsync(
        IReadOnlyList<string> deviceIds,
        IReadOnlyList<string> avgTypes,
        CancellationToken cancellationToken = default);
}
