namespace AirQoon.Web.Models;

public class ConversationContext
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    public string? CurrentIntent { get; set; }

    public Dictionary<string, string> CollectedParameters { get; set; } = new();

    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public string? TenantSlug { get; set; }

    public string? Domain { get; set; }

    public string? Pollutant { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Month1 { get; set; }

    public string? Month2 { get; set; }

    public int TenantInvalidAttempts { get; set; } = 0;
}
