using AirQoon.Web.Models.Dtos;

namespace AirQoon.Web.Services;

public interface IVectorDbService
{
    Task EnsureTenantCollectionAsync(string tenantSlug, CancellationToken cancellationToken = default);

    Task<string> SaveAnalysisAsync(
        string tenantSlug,
        string analysisText,
        string analysisType = "analysis",
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnalysisSearchResult>> SearchAnalysisAsync(
        string tenantSlug,
        string queryText,
        int limit = 5,
        double scoreThreshold = 0.5,
        string? filterType = null,
        CancellationToken cancellationToken = default);
}
