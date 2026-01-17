namespace AirQoon.Web.Models.Dtos;

public class AirQualityAggregate
{
    public string Parameter { get; set; } = string.Empty;

    public double? Average { get; set; }

    public double? Minimum { get; set; }

    public double? Maximum { get; set; }

    public long MeasurementCount { get; set; }

    public string? Unit { get; set; }
}
