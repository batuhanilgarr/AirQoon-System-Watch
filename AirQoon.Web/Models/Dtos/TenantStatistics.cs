namespace AirQoon.Web.Models.Dtos;

public class TenantStatistics
{
    public string? TenantSlug { get; set; }

    public string? TenantName { get; set; }

    public int DeviceCount { get; set; }

    public long VectorPoints { get; set; }

    public bool IsPublic { get; set; }

    public string? RawText { get; set; }
}
