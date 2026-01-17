using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AirQoon.Web.Data.Entities;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string SessionId { get; set; } = string.Empty;

    [ForeignKey(nameof(SessionId))]
    public ChatSession? Session { get; set; }

    public bool IsUser { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string? ErrorMessage { get; set; }

    public string? IntentType { get; set; }

    public string? ParametersJson { get; set; }

    public string? ResponseDataJson { get; set; }
}
