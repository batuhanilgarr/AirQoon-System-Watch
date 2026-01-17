namespace AirQoon.Web.Models.Chat;

public class IntentDetectionResult
{
    public IntentType Intent { get; set; }

    public string? TenantSlug { get; set; }

    public string? Pollutant { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public string? Month1 { get; set; }

    public string? Month2 { get; set; }
}
