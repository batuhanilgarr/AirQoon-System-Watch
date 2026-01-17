using System.ComponentModel.DataAnnotations;

namespace AirQoon.Web.Data.Entities;

public class DomainTenantMapping
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Domain { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string TenantSlug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
