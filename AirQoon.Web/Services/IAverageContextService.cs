namespace AirQoon.Web.Services;

/// <summary>
/// Service for building context from telemetry averages to enrich prompts.
/// Automatically adds last 30 days averages to system prompts for MCP.
/// </summary>
public interface IAverageContextService
{
    /// <summary>
    /// Builds a context string from telemetry averages for the last 30 days.
    /// This context is automatically added to prompts sent to MCP.
    /// </summary>
    Task<string> BuildAveragesContextAsync(
        string tenantSlug,
        int daysBack = 30,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets summary statistics for all pollutants for a tenant.
    /// </summary>
    Task<Dictionary<string, Dictionary<string, double?>>> GetPollutantSummariesAsync(
        string tenantSlug,
        int daysBack = 30,
        string avgType = "24h_rolling",
        CancellationToken cancellationToken = default);
}
