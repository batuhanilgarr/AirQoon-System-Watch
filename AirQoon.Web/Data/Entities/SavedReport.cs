using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirQoon.Web.Data.Entities;

public class SavedReport
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public AppUser? User { get; set; }

    [MaxLength(255)]
    public string? ReportName { get; set; }

    [MaxLength(50)]
    public string? ReportType { get; set; }

    [MaxLength(255)]
    public string? TenantSlug { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? ReportDataJson { get; set; }

    public string? FilePath { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
