using System.ComponentModel.DataAnnotations;

namespace AirQoon.Web.Data.Entities;

public class DomainAppearance
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Domain { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ChatbotName { get; set; }

    public string? ChatbotLogoUrl { get; set; }

    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    public string? WelcomeMessage { get; set; }

    public bool ChatbotOnline { get; set; } = true;

    public bool OpenChatOnLoad { get; set; } = true;

    public string? QuickRepliesJson { get; set; }

    public string? GreetingResponse { get; set; }

    public string? ThanksResponse { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
