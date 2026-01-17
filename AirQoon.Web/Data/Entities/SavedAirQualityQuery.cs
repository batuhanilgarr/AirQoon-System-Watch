using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirQoon.Web.Data.Entities;

public class SavedAirQualityQuery
{
    [Key]
    public int Id { get; set; }

    [MaxLength(255)]
    public string? SessionId { get; set; }

    [ForeignKey(nameof(SessionId))]
    public ChatSession? Session { get; set; }

    [MaxLength(50)]
    public string? QueryType { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? Pollutant { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? ParametersJson { get; set; }

    public string? ResultSummary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
