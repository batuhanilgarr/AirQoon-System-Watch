namespace AirQoon.Web.Models.Dtos;

public class MonthlyComparisonResult
{
    public string? TenantSlug { get; set; }

    public string? TenantName { get; set; }

    public string? Month1 { get; set; }

    public string? Month2 { get; set; }

    public int DeviceCount { get; set; }

    public List<PollutantComparison> Comparisons { get; set; } = new();

    public string? VectorId { get; set; }

    public string? RawText { get; set; }
}
