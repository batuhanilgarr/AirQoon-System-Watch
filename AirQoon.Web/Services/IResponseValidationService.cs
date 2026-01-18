namespace AirQoon.Web.Services;

/// <summary>
/// Service for validating and filtering chat responses based on tenant-specific rules.
/// Implements eval/validation system to prevent inappropriate responses.
/// </summary>
public interface IResponseValidationService
{
    /// <summary>
    /// Validates a response before sending to user.
    /// Returns the original response if valid, or a filtered/modified response if needed.
    /// </summary>
    Task<string> ValidateResponseAsync(
        string response, 
        string? tenantSlug, 
        string userMessage,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a response should be restricted based on tenant rules.
    /// </summary>
    bool ShouldRestrictResponse(string response, string? tenantSlug);
}
