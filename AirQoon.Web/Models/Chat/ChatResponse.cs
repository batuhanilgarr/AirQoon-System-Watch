namespace AirQoon.Web.Models.Chat;

public class ChatResponse
{
    public string SessionId { get; set; } = string.Empty;

    public string Reply { get; set; } = string.Empty;

    public IntentType Intent { get; set; }

    public string? TenantSlug { get; set; }
}
