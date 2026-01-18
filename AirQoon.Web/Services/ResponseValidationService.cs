using System.Text.RegularExpressions;

namespace AirQoon.Web.Services;

/// <summary>
/// Implements eval/validation system for chat responses.
/// Prevents inappropriate responses based on tenant-specific rules.
/// </summary>
public class ResponseValidationService : IResponseValidationService
{
    private readonly ILogger<ResponseValidationService> _logger;
    
    // Global restricted topics (apply to all tenants)
    private static readonly HashSet<string> GlobalRestrictedTopics = new(StringComparer.OrdinalIgnoreCase)
    {
        "çevre aktivizmi",
        "environmental activism",
        "politik",
        "political",
        "protesto",
        "protest"
    };
    
    // Tenant-specific restrictions
    private static readonly Dictionary<string, TenantRestrictions> TenantRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["akcansa"] = new TenantRestrictions
        {
            RestrictedTopics = new[] { "çevre", "environment", "emisyon azaltma önerileri" },
            ToneGuidelines = "Sertlikte konuşmamak, profesyonel ve objektif olmak",
            MaxSeverityLevel = "info" // Don't use alarming language
        },
        ["tupras"] = new TenantRestrictions
        {
            RestrictedTopics = new[] { "rafineri operasyonları", "refinery operations" },
            ToneGuidelines = "Teknik ve objektif, spekülasyon yapmamak",
            MaxSeverityLevel = "info"
        }
    };
    
    // Patterns that indicate harsh/alarming tone
    private static readonly Regex[] HarshTonePatterns = new[]
    {
        new Regex(@"\b(tehlikeli|dangerous|kritik|critical|alarm|acil|emergency|felaket|disaster)\b", RegexOptions.IgnoreCase),
        new Regex(@"!!+", RegexOptions.None),
        new Regex(@"\b(ÇOK YÜKSEK|VERY HIGH|EXTREMELY|SON DERECE)\b", RegexOptions.IgnoreCase)
    };

    public ResponseValidationService(ILogger<ResponseValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ValidateResponseAsync(
        string response, 
        string? tenantSlug, 
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return response;
        }

        // Check for global restrictions
        if (ContainsGlobalRestrictedContent(response))
        {
            _logger.LogWarning("Response contains globally restricted content. Filtering.");
            return FilterGlobalRestrictedContent(response);
        }

        // Check tenant-specific restrictions
        if (!string.IsNullOrWhiteSpace(tenantSlug) && TenantRules.TryGetValue(tenantSlug, out var rules))
        {
            if (ContainsRestrictedTopics(response, rules.RestrictedTopics))
            {
                _logger.LogWarning("Response contains tenant-restricted topics for {TenantSlug}. Filtering.", tenantSlug);
                return FilterRestrictedTopics(response, rules.RestrictedTopics);
            }

            if (HasHarshTone(response))
            {
                _logger.LogWarning("Response has harsh tone for {TenantSlug}. Softening.", tenantSlug);
                response = SoftenTone(response);
            }
        }

        return await Task.FromResult(response);
    }

    public bool ShouldRestrictResponse(string response, string? tenantSlug)
    {
        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        // Check global restrictions
        if (ContainsGlobalRestrictedContent(response))
        {
            return true;
        }

        // Check tenant-specific restrictions
        if (!string.IsNullOrWhiteSpace(tenantSlug) && TenantRules.TryGetValue(tenantSlug, out var rules))
        {
            if (ContainsRestrictedTopics(response, rules.RestrictedTopics))
            {
                return true;
            }

            if (HasHarshTone(response))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsGlobalRestrictedContent(string text)
    {
        var lowerText = text.ToLowerInvariant();
        return GlobalRestrictedTopics.Any(topic => lowerText.Contains(topic.ToLowerInvariant()));
    }

    private static bool ContainsRestrictedTopics(string text, string[] restrictedTopics)
    {
        if (restrictedTopics == null || restrictedTopics.Length == 0)
        {
            return false;
        }

        var lowerText = text.ToLowerInvariant();
        return restrictedTopics.Any(topic => lowerText.Contains(topic.ToLowerInvariant()));
    }

    private static bool HasHarshTone(string text)
    {
        return HarshTonePatterns.Any(pattern => pattern.IsMatch(text));
    }

    private static string FilterGlobalRestrictedContent(string text)
    {
        return "Üzgünüm, sadece hava kalitesi ölçüm verileri ve analizleri hakkında bilgi verebilirim. " +
               "Lütfen hava kalitesi ile ilgili teknik sorular sorun.";
    }

    private static string FilterRestrictedTopics(string text, string[] restrictedTopics)
    {
        // Remove paragraphs containing restricted topics
        var lines = text.Split('\n');
        var filtered = new List<string>();

        foreach (var line in lines)
        {
            var lowerLine = line.ToLowerInvariant();
            var containsRestricted = restrictedTopics.Any(topic => lowerLine.Contains(topic.ToLowerInvariant()));
            
            if (!containsRestricted)
            {
                filtered.Add(line);
            }
        }

        var result = string.Join('\n', filtered).Trim();
        
        // If too much was filtered, return a safe default
        if (result.Length < text.Length / 3)
        {
            return "Hava kalitesi verileri analiz edildi. Detaylı bilgi için lütfen spesifik bir parametre belirtin (örn: PM10, PM2.5, NO2).";
        }

        return result;
    }

    private static string SoftenTone(string text)
    {
        // Replace harsh words with softer alternatives
        var softened = text;
        
        softened = Regex.Replace(softened, @"\btehlikeli\b", "yüksek", RegexOptions.IgnoreCase);
        softened = Regex.Replace(softened, @"\bdangerous\b", "elevated", RegexOptions.IgnoreCase);
        softened = Regex.Replace(softened, @"\bkritik\b", "dikkat edilmesi gereken", RegexOptions.IgnoreCase);
        softened = Regex.Replace(softened, @"\bcritical\b", "notable", RegexOptions.IgnoreCase);
        softened = Regex.Replace(softened, @"\balarm\b", "bilgi", RegexOptions.IgnoreCase);
        softened = Regex.Replace(softened, @"\bacil\b", "önemli", RegexOptions.IgnoreCase);
        softened = Regex.Replace(softened, @"\bemergency\b", "important", RegexOptions.IgnoreCase);
        
        // Remove excessive exclamation marks
        softened = Regex.Replace(softened, @"!!+", ".", RegexOptions.None);
        
        // Replace all-caps emphasis
        softened = Regex.Replace(softened, @"\b(ÇOK YÜKSEK|VERY HIGH|EXTREMELY|SON DERECE)\b", 
            match => char.ToUpper(match.Value[0]) + match.Value.Substring(1).ToLower(), 
            RegexOptions.IgnoreCase);
        
        return softened;
    }

    private class TenantRestrictions
    {
        public string[] RestrictedTopics { get; set; } = Array.Empty<string>();
        public string ToneGuidelines { get; set; } = string.Empty;
        public string MaxSeverityLevel { get; set; } = "info";
    }
}
