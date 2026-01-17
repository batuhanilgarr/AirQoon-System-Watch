using AirQoon.Web.Models.Chat;

namespace AirQoon.Web.Services;

public interface ILlmService
{
    string BuildTenantAwareIntentPrompt(string userMessage, string? domain, string? tenantSlug);

    Task<IntentDetectionResult> DetectIntentAsync(
        string userMessage,
        string? domain,
        string? tenantSlug,
        CancellationToken cancellationToken = default);
}
