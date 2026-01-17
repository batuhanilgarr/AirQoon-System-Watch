using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirQoon.Web.Data.Entities;

public class ConversationContextEntity
{
    [Key]
    [MaxLength(255)]
    public string SessionId { get; set; } = string.Empty;

    [ForeignKey(nameof(SessionId))]
    public ChatSession? Session { get; set; }

    [MaxLength(50)]
    public string? CurrentIntent { get; set; }

    public string? CollectedParametersJson { get; set; }

    [MaxLength(255)]
    public string? Domain { get; set; }

    [MaxLength(255)]
    public string? TenantSlug { get; set; }

    [MaxLength(50)]
    public string? Pollutant { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [MaxLength(10)]
    public string? Month1 { get; set; }

    [MaxLength(10)]
    public string? Month2 { get; set; }

    public int TenantInvalidAttempts { get; set; } = 0;

    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
