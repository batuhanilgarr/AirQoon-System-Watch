using System.Globalization;
using System.Text.RegularExpressions;
using AirQoon.Web.Models.Chat;

namespace AirQoon.Web.Services;

public class LlmService : ILlmService
{
    private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ocak"] = 1,
        ["subat"] = 2,
        ["mart"] = 3,
        ["nisan"] = 4,
        ["mayis"] = 5,
        ["haziran"] = 6,
        ["temmuz"] = 7,
        ["agustos"] = 8,
        ["eylul"] = 9,
        ["ekim"] = 10,
        ["kasim"] = 11,
        ["aralik"] = 12
    };

    public string BuildTenantAwareIntentPrompt(string userMessage, string? domain, string? tenantSlug)
    {
        // Prompt is stored/edited in AdminSettings in later phases.
        // For now we expose a deterministic tenant-aware template.
        return $"""
SENARYO: AirQoon Hava Kalitesi Chatbot

Bağlam:
- Domain: {domain}
- TenantSlug (varsa): {tenantSlug}

Kullanıcı mesajı:
{userMessage}

Görev:
1) Intent seç: AirQualityQuery | StatisticalAnalysis | ComparisonAnalysis | ReportRequest | Unknown
2) Parametreleri çıkar:
   - tenantSlug (slug formatında)
   - pollutant (PM10, PM2.5, NO2, SO2, O3, CO)
   - startDate/endDate (YYYY-MM-DD)
   - month1/month2 (YYYY-MM)

Kurallar:
- Tenant adı verilirse slug'a çevir.
- Kirletici kısaltmalarını normalize et (PM25 -> PM2.5).
""";
    }

    public Task<IntentDetectionResult> DetectIntentAsync(
        string userMessage,
        string? domain,
        string? tenantSlug,
        CancellationToken cancellationToken = default)
    {
        var result = new IntentDetectionResult
        {
            TenantSlug = tenantSlug
        };

        var m = (userMessage ?? string.Empty).Trim();
        var lower = m.ToLowerInvariant();

        if (Regex.IsMatch(lower, "\\b(karşılaştır|karsilastir|kıyasla|kıyas|fark)\\b"))
        {
            result.Intent = IntentType.ComparisonAnalysis;
        }
        else if (Regex.IsMatch(lower, "\\b(analiz|istatistik|trend|dağılım|dagilim)\\b"))
        {
            result.Intent = IntentType.StatisticalAnalysis;
        }
        else if (Regex.IsMatch(lower, "\\b(pm10|pm2\\.5|pm25|no2|so2|o3|co)\\b"))
        {
            result.Intent = IntentType.AirQualityQuery;
        }
        else
        {
            result.Intent = IntentType.Unknown;
        }

        result.Pollutant = NormalizePollutantToken(ExtractPollutantToken(m));

        var (start, end) = ExtractDateRangeUtc(m);
        result.StartDateUtc = start;
        result.EndDateUtc = end;

        var (month1, month2) = ExtractMonths(m);
        result.Month1 = month1;
        result.Month2 = month2;

        return Task.FromResult(result);
    }

    public static string? NormalizePollutantToken(string? pollutant)
    {
        if (string.IsNullOrWhiteSpace(pollutant))
        {
            return null;
        }

        var p = pollutant.Trim().ToUpperInvariant();
        if (p == "PM25") return "PM2.5";
        return p;
    }

    public static string NormalizePollutantDbParameter(string pollutant)
    {
        var p = NormalizePollutantToken(pollutant) ?? pollutant;
        return p switch
        {
            "PM10" => "PM10-24h",
            "PM2.5" => "PM2.5-24h",
            "NO2" => "NO2-1h",
            "O3" => "O3-1h",
            "SO2" => "SO2-1h",
            "CO" => "CO-8h",
            _ => p
        };
    }

    public static string NormalizeTenantSlug(string tenantSlug)
    {
        var s = tenantSlug.Trim().ToLowerInvariant();
        s = Regex.Replace(s, "\\s+", "-");
        s = Regex.Replace(s, "[^a-z0-9-]", string.Empty);
        s = Regex.Replace(s, "-+", "-");
        return s.Trim('-');
    }

    public static string ConvertTenantNameToSlug(string tenantName)
    {
        var s = tenantName.Trim().ToLowerInvariant();

        s = s.Replace('ı', 'i');
        s = s.Replace('ğ', 'g');
        s = s.Replace('ü', 'u');
        s = s.Replace('ş', 's');
        s = s.Replace('ö', 'o');
        s = s.Replace('ç', 'c');

        s = Regex.Replace(s, "\\s+", "-");
        s = Regex.Replace(s, "[^a-z0-9-]", string.Empty);
        s = Regex.Replace(s, "-+", "-");

        return s.Trim('-');
    }

    private static string? ExtractPollutantToken(string message)
    {
        var upper = message.ToUpperInvariant();
        if (upper.Contains("PM2.5") || upper.Contains("PM25")) return "PM2.5";
        if (upper.Contains("PM10")) return "PM10";
        if (upper.Contains("NO2")) return "NO2";
        if (upper.Contains("SO2")) return "SO2";
        if (upper.Contains("O3")) return "O3";
        if (Regex.IsMatch(upper, "\\bCO\\b")) return "CO";
        return null;
    }

    private static (DateTime? startUtc, DateTime? endUtc) ExtractDateRangeUtc(string message)
    {
        var range = Regex.Match(message, "(?<y1>\\d{4})-(?<m1>\\d{2})-(?<d1>\\d{2}).*(?<y2>\\d{4})-(?<m2>\\d{2})-(?<d2>\\d{2})");
        if (range.Success)
        {
            if (DateTime.TryParseExact($"{range.Groups["y1"].Value}-{range.Groups["m1"].Value}-{range.Groups["d1"].Value}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var s) &&
                DateTime.TryParseExact($"{range.Groups["y2"].Value}-{range.Groups["m2"].Value}-{range.Groups["d2"].Value}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var e))
            {
                var start = DateTime.SpecifyKind(s.Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(e.Date.AddDays(1), DateTimeKind.Utc);
                return (start, end);
            }
        }

        var single = Regex.Match(message, "(?<y>\\d{4})-(?<m>\\d{2})-(?<d>\\d{2})");
        if (single.Success)
        {
            if (DateTime.TryParseExact($"{single.Groups["y"].Value}-{single.Groups["m"].Value}-{single.Groups["d"].Value}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d))
            {
                var start = DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(d.Date.AddDays(1), DateTimeKind.Utc);
                return (start, end);
            }
        }

        return (null, null);
    }

    private static (string? month1, string? month2) ExtractMonths(string message)
    {
        var matches = Regex.Matches(message, "(?<y>\\d{4})-(?<m>\\d{2})");
        if (matches.Count >= 2)
        {
            return (matches[0].Value, matches[1].Value);
        }

        var normalized = message.ToLowerInvariant();
        normalized = normalized.Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');

        var nameMatches = Regex.Matches(normalized, "(?<mon>ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\\s+(?<year>\\d{4})");
        if (nameMatches.Count >= 2)
        {
            var y1 = int.Parse(nameMatches[0].Groups["year"].Value);
            var m1 = MonthMap[nameMatches[0].Groups["mon"].Value];
            var y2 = int.Parse(nameMatches[1].Groups["year"].Value);
            var m2 = MonthMap[nameMatches[1].Groups["mon"].Value];
            return ($"{y1:D4}-{m1:D2}", $"{y2:D4}-{m2:D2}");
        }

        return (null, null);
    }
}
