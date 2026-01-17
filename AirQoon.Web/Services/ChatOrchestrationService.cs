using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AirQoon.Web.Data;
using AirQoon.Web.Data.Entities;
using AirQoon.Web.Models.Chat;
using AirQoon.Web.Services.MongoModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AirQoon.Web.Services;

public class ChatOrchestrationService : IChatOrchestrationService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantMappingService _tenantMapping;
    private readonly IMongoDbService _mongo;
    private readonly IPostgresAirQualityService _airQuality;
    private readonly IAirQualityMcpService _mcp;
    private readonly ILlmService _llm;
    private readonly ILogger<ChatOrchestrationService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ChatOrchestrationService(
        ApplicationDbContext db,
        ITenantMappingService tenantMapping,
        IMongoDbService mongo,
        IPostgresAirQualityService airQuality,
        IAirQualityMcpService mcp,
        ILlmService llm,
        ILogger<ChatOrchestrationService> logger)
    {
        _db = db;
        _tenantMapping = tenantMapping;
        _mongo = mongo;
        _airQuality = airQuality;
        _mcp = mcp;
        _llm = llm;
        _logger = logger;
    }

    public async Task<ChatResponse> HandleMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var session = await EnsureSessionAsync(request, cancellationToken);

        var userMessage = request.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return new ChatResponse
            {
                SessionId = session.SessionId,
                Reply = "Mesaj boş olamaz.",
                Intent = IntentType.Unknown,
                TenantSlug = session.TenantSlug
            };
        }

        var context = await EnsureConversationContextAsync(session, cancellationToken);

        var detectedTenant = await ExtractTenantSlugAsync(request, userMessage, context, cancellationToken);
        if (!string.IsNullOrWhiteSpace(detectedTenant))
        {
            session.TenantSlug = detectedTenant;
            context.TenantSlug = detectedTenant;
        }

        var intent = DetectIntent(userMessage);
        try
        {
            var llmResult = await _llm.DetectIntentAsync(userMessage, session.Domain, session.TenantSlug, cancellationToken);
            if (llmResult.Intent != IntentType.Unknown)
            {
                intent = llmResult.Intent;
            }

            if (!string.IsNullOrWhiteSpace(llmResult.TenantSlug))
            {
                session.TenantSlug = NormalizeTenantSlug(llmResult.TenantSlug);
                context.TenantSlug = session.TenantSlug;
            }
        }
        catch
        {
            // LLM detection is best-effort; fallback to heuristic
        }

        // Guardrail: for queries like "son gün hava kalitesini göster" without a pollutant,
        // prefer StatisticalAnalysis (multi-pollutant summary) instead of single-pollutant AirQualityQuery.
        var normalizedMessage = userMessage.ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');
        var hasPollutant = Regex.IsMatch(userMessage, "\\b(pm10|pm2\\.5|pm25|no2|so2|o3|co)\\b", RegexOptions.IgnoreCase);
        if ((normalizedMessage.Contains("hava kalitesi") || normalizedMessage.Contains("hava kalitesini")) && !hasPollutant)
        {
            intent = IntentType.StatisticalAnalysis;
        }

        var userEntity = new ChatMessage
        {
            SessionId = session.SessionId,
            IsUser = true,
            Content = userMessage,
            Timestamp = DateTime.UtcNow,
            IntentType = intent.ToString()
        };
        _db.ChatMessages.Add(userEntity);

        string reply;
        string? responseJson = null;
        string? errorMessage = null;
        Dictionary<string, object?>? parameters = null;

        try
        {
            if (intent == IntentType.AirQualityQuery)
            {
                var result = await HandleAirQualityQueryAsync(session, context, userMessage, cancellationToken);
                reply = result.reply;
                parameters = result.parameters;
            }
            else if (intent == IntentType.StatisticalAnalysis)
            {
                var result = await HandleStatisticalAnalysisAsync(session, context, userMessage, cancellationToken);
                reply = result.reply;
                parameters = result.parameters;
            }
            else if (intent == IntentType.ComparisonAnalysis)
            {
                var result = await HandleMonthlyComparisonAsync(session, context, userMessage, cancellationToken);
                reply = result.reply;
                parameters = result.parameters;
            }
            else
            {
                reply = "Üzgünüm, sadece hava kalitesi ölçüm verileri ve analizleri hakkında sorulara cevap verebilirim.";
            }

            if (!string.IsNullOrWhiteSpace(session.TenantSlug))
            {
                var rag = await BuildRagEnrichmentAsync(session.TenantSlug, userMessage, cancellationToken);
                if (!string.IsNullOrWhiteSpace(rag))
                {
                    reply += $"\n\n---\n\n## İlgili önceki analizler (RAG)\n\n{rag}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat message handling failed. SessionId={SessionId} Tenant={TenantSlug} Intent={Intent}", session.SessionId, session.TenantSlug, intent);
            errorMessage = ex.ToString();
            responseJson = JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);

            reply = "Bir hata oluştu. Lütfen tekrar deneyin.";
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                var msg = ex.Message;
                if (msg.Length > 240)
                {
                    msg = msg[..240];
                }
                reply = $"Hata: {msg}";
            }
        }

        reply = CleanReply(reply);

        userEntity.ParametersJson = parameters is null ? null : JsonSerializer.Serialize(parameters, JsonOptions);

        var assistantEntity = new ChatMessage
        {
            SessionId = session.SessionId,
            IsUser = false,
            Content = reply,
            Timestamp = DateTime.UtcNow,
            IntentType = intent.ToString(),
            ErrorMessage = errorMessage,
            ResponseDataJson = responseJson
        };
        _db.ChatMessages.Add(assistantEntity);

        session.LastActivityAt = DateTime.UtcNow;
        context.LastActivity = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new ChatResponse
        {
            SessionId = session.SessionId,
            Reply = reply,
            Intent = intent,
            TenantSlug = session.TenantSlug
        };
    }

    private static string CleanReply(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Remove MCP "vector DB'e kaydedildi" informational blocks from the visible reply.
        text = Regex.Replace(text, @"(?is)\n*✅\s*\*\*Analiz\s+vector\s+database['’]?e\s+kaydedildi\*\*.*?(\n\n|$)", "\n\n");
        text = Regex.Replace(text, @"(?im)^Artık\s+RAG\s+ile\s+arama\s+yapabilirsiniz\.?\s*$", string.Empty);

        // If RAG section exists but says 0 results, drop the whole RAG section.
        if (Regex.IsMatch(text, @"(?is)##\s*İlgili\s+önceki\s+analizler\s*\(RAG\).*?(Bulunan\s+Sonuç\s*:\s*0|0\s+adet|bulunamadı)"))
        {
            text = Regex.Replace(text, @"(?is)\n\n---\n\n##\s*İlgili\s+önceki\s+analizler\s*\(RAG\)\s*.*$", string.Empty);
        }

        return text.Trim();
    }

    private async Task<string?> BuildRagEnrichmentAsync(string tenantSlug, string queryText, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            var results = await _mcp.SearchAnalysisFromVectorDbAsync(
                tenantSlug,
                queryText,
                limit: 3,
                scoreThreshold: 0.5,
                filterType: null,
                cancellationToken: cts.Token);

            if (results.Count == 0)
            {
                return null;
            }

            // Our MCP client returns formatted markdown text in the first result.
            // Keep it short to avoid flooding chat.
            var text = results[0].Text ?? string.Empty;

            // Some MCP responses include a formatted "0 results" message; treat it as empty.
            if (Regex.IsMatch(text, @"(?is)(Bulunan\s+Sonuç\s*:\s*0|0\s+adet|bulunamadı)") )
            {
                return null;
            }

            if (text.Length > 1200)
            {
                text = text[..1200] + "...";
            }

            return text;
        }
        catch
        {
            return null;
        }
    }

    private IntentType DetectIntent(string message)
    {
        var m = message.ToLowerInvariant();

        // Normalize Turkish diacritics for robust keyword matching
        var norm = m.Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');

        var hasPollutant = Regex.IsMatch(m, "\\b(pm10|pm2\\.5|pm25|no2|so2|o3|co)\\b");

        if (norm.Contains("hava kalitesi") || norm.Contains("hava kalitesini"))
        {
            return hasPollutant ? IntentType.AirQualityQuery : IntentType.StatisticalAnalysis;
        }

        if (Regex.IsMatch(m, "\\b(karşılaştır|karsilastir|kıyasla|kıyas|fark)\\b"))
        {
            return IntentType.ComparisonAnalysis;
        }

        if (Regex.IsMatch(m, "\\b(analiz|istatistik|trend|dağılım|dagilim)\\b"))
        {
            return IntentType.StatisticalAnalysis;
        }

        if (Regex.IsMatch(m, "\\b(pm10|pm2\\.5|pm25|no2|so2|o3|co)\\b"))
        {
            return IntentType.AirQualityQuery;
        }

        return IntentType.Unknown;
    }

    private async Task<ChatSession> EnsureSessionAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var sessionId = string.IsNullOrWhiteSpace(request.SessionId) ? Guid.NewGuid().ToString() : request.SessionId;

        var session = await _db.ChatSessions.FirstOrDefaultAsync(x => x.SessionId == sessionId, cancellationToken);
        if (session is null)
        {
            session = new ChatSession
            {
                SessionId = sessionId,
                Domain = request.Domain,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true
            };
            _db.ChatSessions.Add(session);
        }
        else
        {
            session.Domain ??= request.Domain;
            session.IpAddress ??= request.IpAddress;
            session.UserAgent ??= request.UserAgent;
        }

        if (string.IsNullOrWhiteSpace(session.TenantSlug) && !string.IsNullOrWhiteSpace(request.Domain))
        {
            var mapped = await _tenantMapping.GetTenantSlugForDomainAsync(request.Domain, cancellationToken);
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                session.TenantSlug = mapped;
            }
        }

        return session;
    }

    private async Task<ConversationContextEntity> EnsureConversationContextAsync(ChatSession session, CancellationToken cancellationToken)
    {
        var context = await _db.ConversationContexts.FirstOrDefaultAsync(x => x.SessionId == session.SessionId, cancellationToken);
        if (context is null)
        {
            context = new ConversationContextEntity
            {
                SessionId = session.SessionId,
                Domain = session.Domain,
                TenantSlug = session.TenantSlug,
                LastActivity = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CollectedParametersJson = JsonSerializer.Serialize(new Dictionary<string, object>(), JsonOptions)
            };
            _db.ConversationContexts.Add(context);
        }
        else
        {
            context.Domain ??= session.Domain;
            context.TenantSlug ??= session.TenantSlug;
        }

        return context;
    }

    private async Task<string?> ExtractTenantSlugAsync(ChatRequest request, string message, ConversationContextEntity context, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            var normalized = NormalizeTenantSlug(request.TenantSlug);
            if (await _mongo.TenantExistsAsync(normalized, cancellationToken))
            {
                return normalized;
            }
        }

        if (!string.IsNullOrWhiteSpace(context.TenantSlug))
        {
            return context.TenantSlug;
        }

        if (!string.IsNullOrWhiteSpace(request.Domain))
        {
            var mapped = await _tenantMapping.GetTenantSlugForDomainAsync(request.Domain, cancellationToken);
            if (!string.IsNullOrWhiteSpace(mapped) && await _mongo.TenantExistsAsync(mapped, cancellationToken))
            {
                return mapped;
            }
        }

        var slugCandidate = Regex.Match(message, "\\b([a-z0-9]+(?:-[a-z0-9]+)*)\\b", RegexOptions.IgnoreCase).Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(slugCandidate))
        {
            var normalized = NormalizeTenantSlug(slugCandidate);
            if (await _mongo.TenantExistsAsync(normalized, cancellationToken))
            {
                return normalized;
            }
        }

        // Fallback: try to match tenant names by scanning a limited subset
        var maybeName = ExtractTenantNameFromText(message);
        if (!string.IsNullOrWhiteSpace(maybeName))
        {
            var slug = ConvertTenantNameToSlug(maybeName);
            if (await _mongo.TenantExistsAsync(slug, cancellationToken))
            {
                return slug;
            }

            // try to find by Name equality (case-insensitive)
            var tenant = await FindTenantByNameAsync(maybeName, cancellationToken);
            if (tenant is not null)
            {
                return tenant.SlugName;
            }
        }

        return null;
    }

    private static string NormalizeTenantSlug(string tenantSlug)
    {
        return LlmService.NormalizeTenantSlug(tenantSlug);
    }

    private static string ConvertTenantNameToSlug(string tenantName)
    {
        return LlmService.ConvertTenantNameToSlug(tenantName);
    }

    private static string? ExtractTenantNameFromText(string message)
    {
        var m = Regex.Match(message, "\\b([A-ZÇĞİÖŞÜ][^\\n]{1,60})\\b");
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private async Task<TenantInfo?> FindTenantByNameAsync(string name, CancellationToken cancellationToken)
    {
        // cheap approach: iterate devices/tenants not available via IMongoDbService currently; keep simple by checking slug conversion.
        // If needed, extend IMongoDbService later.
        var slug = ConvertTenantNameToSlug(name);
        return await _mongo.GetTenantBySlugAsync(slug, cancellationToken);
    }

    private async Task<(string reply, Dictionary<string, object?> parameters)> HandleAirQualityQueryAsync(
        ChatSession session,
        ConversationContextEntity context,
        string message,
        CancellationToken cancellationToken)
    {
        var tenantSlug = session.TenantSlug ?? context.TenantSlug;
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return ("Hangi tenant için sorgu yapmak istiyorsunuz? (örn: akcansa)", new Dictionary<string, object?> { ["missing"] = "tenantSlug" });
        }

        var pollutant = ExtractPollutant(message) ?? "PM2.5";
        var (start, end) = ExtractDateRangeUtc(message);

        var devices = await _mongo.GetDevicesByTenantSlugAsync(tenantSlug, 500, cancellationToken);
        var deviceIds = devices.Select(d => d.DeviceId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (deviceIds.Count == 0)
        {
            return ($"{tenantSlug} için cihaz bulunamadı.", new Dictionary<string, object?> { ["tenantSlug"] = tenantSlug });
        }

        var aggregates = await _airQuality.GetAggregatesAsync(
            deviceIds,
            start,
            end,
            new[] { pollutant },
            cancellationToken);

        var normalized = _airQuality.NormalizePollutants(new[] { pollutant }).FirstOrDefault() ?? pollutant;

        if (aggregates.Count == 0)
        {
            return ($"{tenantSlug} için {normalized} verisi bulunamadı ({start:yyyy-MM-dd} - {end:yyyy-MM-dd}).", new Dictionary<string, object?>
            {
                ["tenantSlug"] = tenantSlug,
                ["pollutant"] = normalized,
                ["startDate"] = start.ToString("yyyy-MM-dd"),
                ["endDate"] = end.ToString("yyyy-MM-dd")
            });
        }

        var a = aggregates[0];
        var unit = a.Unit ?? "µg/m³";

        var reply = $"{tenantSlug} için {normalized} ({start:yyyy-MM-dd} - {end:yyyy-MM-dd})\n" +
                    $"Ortalama: {Format(a.Average)} {unit}\n" +
                    $"Minimum: {Format(a.Minimum)} {unit}\n" +
                    $"Maksimum: {Format(a.Maximum)} {unit}\n" +
                    $"Ölçüm sayısı: {a.MeasurementCount}";

        // Save this query result to vector DB for later RAG (best-effort)
        try
        {
            var meta = new Dictionary<string, object>
            {
                ["analysis_type"] = "air_quality_query",
                ["tenant_slug"] = tenantSlug,
                ["pollutant"] = normalized,
                ["start_date"] = start.ToString("yyyy-MM-dd"),
                ["end_date"] = end.ToString("yyyy-MM-dd"),
                ["device_count"] = deviceIds.Count
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            await _mcp.SaveAnalysisToVectorDbAsync(
                tenantSlug,
                reply,
                "air_quality_query",
                meta,
                cts.Token);
        }
        catch
        {
        }

        context.Pollutant = normalized;
        context.StartDate = EnsureUtc(start);
        context.EndDate = EnsureUtc(end);

        var parameters = new Dictionary<string, object?>
        {
            ["tenantSlug"] = tenantSlug,
            ["pollutant"] = normalized,
            ["startDate"] = start.ToString("yyyy-MM-dd"),
            ["endDate"] = end.ToString("yyyy-MM-dd"),
            ["deviceCount"] = deviceIds.Count
        };

        return (reply, parameters);
    }

    private async Task<(string reply, Dictionary<string, object?> parameters)> HandleStatisticalAnalysisAsync(
        ChatSession session,
        ConversationContextEntity context,
        string message,
        CancellationToken cancellationToken)
    {
        var tenantSlug = session.TenantSlug ?? context.TenantSlug;
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return ("Hangi tenant için analiz yapalım? (örn: akcansa)", new Dictionary<string, object?> { ["missing"] = "tenantSlug" });
        }

        var (start, end) = ExtractDateRangeUtc(message);

        var pollutants = ExtractPollutants(message);
        if (pollutants.Count == 0)
        {
            pollutants = new List<string> { "PM2.5", "PM10", "NO2" };
        }

        var result = await _mcp.TenantTimeRangeAnalysisAsync(
            tenantSlug,
            start,
            end,
            pollutants,
            null,
            null,
            cancellationToken);

        context.StartDate = EnsureUtc(start);
        context.EndDate = EnsureUtc(end);

        var parameters = new Dictionary<string, object?>
        {
            ["tenantSlug"] = tenantSlug,
            ["startDate"] = start.ToString("yyyy-MM-dd"),
            ["endDate"] = end.ToString("yyyy-MM-dd"),
            ["pollutants"] = pollutants
        };

        return (result.RawText ?? "Analiz tamamlandı.", parameters);
    }

    private async Task<(string reply, Dictionary<string, object?> parameters)> HandleMonthlyComparisonAsync(
        ChatSession session,
        ConversationContextEntity context,
        string message,
        CancellationToken cancellationToken)
    {
        var tenantSlug = session.TenantSlug ?? context.TenantSlug;
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return ("Hangi tenant için aylık karşılaştırma yapalım? (örn: akcansa)", new Dictionary<string, object?> { ["missing"] = "tenantSlug" });
        }

        var (m1, m2) = ExtractMonths(message);
        if (string.IsNullOrWhiteSpace(m1) || string.IsNullOrWhiteSpace(m2))
        {
            return ("Hangi iki ayı karşılaştıralım? (örn: 2025-01 ve 2025-02)", new Dictionary<string, object?> { ["missing"] = "month1/month2" });
        }

        context.Month1 = m1;
        context.Month2 = m2;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(25));

        var result = await _mcp.TenantMonthlyComparisonAsync(tenantSlug, m1, m2, null, cts.Token);

        var parameters = new Dictionary<string, object?>
        {
            ["tenantSlug"] = tenantSlug,
            ["month1"] = m1,
            ["month2"] = m2
        };

        return (result.RawText ?? "Karşılaştırma tamamlandı.", parameters);
    }

    private static string? ExtractPollutant(string message)
    {
        var m = message.ToUpperInvariant();
        if (m.Contains("PM2.5") || m.Contains("PM25")) return "PM2.5";
        if (m.Contains("PM10")) return "PM10";
        if (m.Contains("NO2")) return "NO2";
        if (m.Contains("SO2")) return "SO2";
        if (m.Contains("O3")) return "O3";
        if (Regex.IsMatch(m, "\\bCO\\b")) return "CO";
        return null;
    }

    private static List<string> ExtractPollutants(string message)
    {
        var list = new List<string>();
        foreach (var p in new[] { "PM2.5", "PM25", "PM10", "NO2", "SO2", "O3", "CO" })
        {
            if (Regex.IsMatch(message, $"\\b{Regex.Escape(p)}\\b", RegexOptions.IgnoreCase))
            {
                list.Add(p);
            }
        }

        // normalize duplicates (PM25 -> PM2.5)
        return list
            .Select(x => string.Equals(x, "PM25", StringComparison.OrdinalIgnoreCase) ? "PM2.5" : x)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static (DateTime startUtc, DateTime endUtc) ExtractDateRangeUtc(string message)
    {
        // Default: last 7 days
        var now = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var start = EnsureUtc(now.AddDays(-7));
        var end = EnsureUtc(now.AddDays(1));

        var normalized = message.ToLowerInvariant();
        normalized = normalized.Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');

        // Relative date phrases
        if (normalized.Contains("dun") || normalized.Contains("dün"))
        {
            start = EnsureUtc(now.AddDays(-1));
            end = EnsureUtc(now);
            return (start, end);
        }

        if (normalized.Contains("bugun") || normalized.Contains("bugün"))
        {
            start = EnsureUtc(now);
            end = EnsureUtc(now.AddDays(1));
            return (start, end);
        }

        if (normalized.Contains("son gun") || normalized.Contains("son gün") || normalized.Contains("son 24 saat") || normalized.Contains("son 1 gun"))
        {
            start = EnsureUtc(now.AddDays(-1));
            end = EnsureUtc(now);
            return (start, end);
        }

        var range = Regex.Match(message, "(?<y1>\\d{4})-(?<m1>\\d{2})-(?<d1>\\d{2}).*(?<y2>\\d{4})-(?<m2>\\d{2})-(?<d2>\\d{2})");
        if (range.Success)
        {
            if (DateTime.TryParseExact($"{range.Groups["y1"].Value}-{range.Groups["m1"].Value}-{range.Groups["d1"].Value}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var s) &&
                DateTime.TryParseExact($"{range.Groups["y2"].Value}-{range.Groups["m2"].Value}-{range.Groups["d2"].Value}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var e))
            {
                start = EnsureUtc(DateTime.SpecifyKind(s.Date, DateTimeKind.Utc));
                end = EnsureUtc(DateTime.SpecifyKind(e.Date.AddDays(1), DateTimeKind.Utc));
            }
        }
        else
        {
            var single = Regex.Match(message, "(?<y>\\d{4})-(?<m>\\d{2})-(?<d>\\d{2})");
            if (single.Success)
            {
                if (DateTime.TryParseExact($"{single.Groups["y"].Value}-{single.Groups["m"].Value}-{single.Groups["d"].Value}", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d))
                {
                    start = EnsureUtc(DateTime.SpecifyKind(d.Date, DateTimeKind.Utc));
                    end = EnsureUtc(DateTime.SpecifyKind(d.Date.AddDays(1), DateTimeKind.Utc));
                }
            }
        }

        return (start, end);
    }

    private static DateTime EnsureUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => dt
        };
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

        var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
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

        // e.g. "ocak 2025" / "subat 2025"
        var nameMatches = Regex.Matches(normalized, "(?<mon>ocak|subat|mart|nisan|mayis|haziran|temmuz|agustos|eylul|ekim|kasim|aralik)\\s+(?<year>\\d{4})");
        if (nameMatches.Count >= 2)
        {
            var y1 = int.Parse(nameMatches[0].Groups["year"].Value);
            var m1 = monthMap[nameMatches[0].Groups["mon"].Value];
            var y2 = int.Parse(nameMatches[1].Groups["year"].Value);
            var m2 = monthMap[nameMatches[1].Groups["mon"].Value];
            return ($"{y1:D4}-{m1:D2}", $"{y2:D4}-{m2:D2}");
        }

        // relative terms: "bu ay" vs "gecen ay"
        if (normalized.Contains("bu ay") || normalized.Contains("gecen ay") || normalized.Contains("geçen ay"))
        {
            var now = DateTime.UtcNow;
            var thisMonth = $"{now:yyyy-MM}";
            var prev = now.AddMonths(-1);
            var prevMonth = $"{prev:yyyy-MM}";
            return (prevMonth, thisMonth);
        }

        return (null, null);
    }

    private static string Format(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : "N/A";
    }
}
