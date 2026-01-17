namespace AirQoon.Web.Models.Dtos;

public class TimeRangeAnalysisResult
{
    public string? TenantSlug { get; set; }

    public string? TenantName { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? ComparisonStartDate { get; set; }

    public DateTime? ComparisonEndDate { get; set; }

    public int DeviceCount { get; set; }

    public List<PollutantAggregate> Aggregates { get; set; } = new();

    public List<PollutantComparison> Comparisons { get; set; } = new();

    public string? VectorId { get; set; }

    public string? RawText { get; set; }
}

public class PollutantAggregate
{
    public string Parameter { get; set; } = string.Empty;

    public double? Average { get; set; }

    public double? Minimum { get; set; }

    public double? Maximum { get; set; }

    public string? Unit { get; set; }

    public long MeasurementCount { get; set; }
}

public class PollutantComparison
{
    public string Parameter { get; set; } = string.Empty;

    public double? PreviousAverage { get; set; }

    public double? CurrentAverage { get; set; }

    public double? Difference { get; set; }

    public double? DifferencePercent { get; set; }

    public bool IsDramaticChange { get; set; }
}
