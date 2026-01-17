using System.ComponentModel.DataAnnotations;

namespace AirQoon.Web.Data.Entities;

public class AdminSetting
{
    [Key]
    public int Id { get; set; } = 1;

    [Required]
    [MaxLength(50)]
    public string LlmProvider { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ModelName { get; set; }

    public string? ApiKey { get; set; }

    [MaxLength(255)]
    public string? OllamaBaseUrl { get; set; }

    public string? SystemPrompt { get; set; }

    public decimal Temperature { get; set; } = 0.7m;

    public int MaxTokens { get; set; } = 2000;

    [MaxLength(255)]
    public string? ApiBaseUrl { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
