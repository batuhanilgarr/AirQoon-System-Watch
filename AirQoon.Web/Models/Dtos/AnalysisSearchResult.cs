namespace AirQoon.Web.Models.Dtos;

public class AnalysisSearchResult
{
    public double Score { get; set; }

    public string? Text { get; set; }

    public string? AnalysisType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}
