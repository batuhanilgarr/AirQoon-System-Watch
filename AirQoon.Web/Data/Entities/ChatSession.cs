using System.ComponentModel.DataAnnotations;

namespace AirQoon.Web.Data.Entities;

public class ChatSession
{
    [Key]
    [MaxLength(255)]
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    [MaxLength(255)]
    public string? Domain { get; set; }

    [MaxLength(255)]
    public string? TenantSlug { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastActivityAt { get; set; }

    public bool IsActive { get; set; } = true;

    public List<ChatMessage> Messages { get; set; } = new();

    public ConversationContextEntity? ConversationContext { get; set; }
}
