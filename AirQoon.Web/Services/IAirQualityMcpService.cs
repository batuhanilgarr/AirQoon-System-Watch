using AirQoon.Web.Models.Dtos;

namespace AirQoon.Web.Services;

public interface IAirQualityMcpService
{
    Task<TimeRangeAnalysisResult> TenantTimeRangeAnalysisAsync(
        string tenantSlug,
        DateTime startDate,
        DateTime endDate,
        List<string>? pollutants = null,
        DateTime? comparisonStartDate = null,
        DateTime? comparisonEndDate = null,
        CancellationToken cancellationToken = default);

    Task<MonthlyComparisonResult> TenantMonthlyComparisonAsync(
        string tenantSlug,
        string month1,
        string month2,
        int? year = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceInfo>> GetTenantDevicesAsync(string tenantSlug, CancellationToken cancellationToken = default);

    Task<TenantStatistics> GetTenantStatisticsAsync(string tenantSlug, CancellationToken cancellationToken = default);

    Task<string> SaveAnalysisToVectorDbAsync(
        string tenantSlug,
        string analysisText,
        string analysisType = "analysis",
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalysisSearchResult>> SearchAnalysisFromVectorDbAsync(
        string tenantSlug,
        string queryText,
        int limit = 5,
        double scoreThreshold = 0.5,
        string? filterType = null,
        CancellationToken cancellationToken = default);
}
