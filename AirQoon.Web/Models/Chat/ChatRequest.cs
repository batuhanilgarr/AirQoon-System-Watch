namespace AirQoon.Web.Models.Chat;

public class ChatRequest
{
    public string? SessionId { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Domain { get; set; }

    public string? TenantSlug { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }
}
