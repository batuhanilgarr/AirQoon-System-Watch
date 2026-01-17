using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AirQoon.Web.Models.Dtos;

namespace AirQoon.Web.Services;

public class VectorDbService : IVectorDbService
{
    private const int DefaultVectorSize = 384;

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;

    public VectorDbService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        _configuration = configuration;

        var host = _configuration["Qdrant:Host"];
        if (!string.IsNullOrWhiteSpace(host) && _http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(host);
        }

        if (_http.BaseAddress is null)
        {
            throw new InvalidOperationException("Qdrant:Host configuration is missing (or HttpClient BaseAddress not set).");
        }
    }

    public async Task EnsureTenantCollectionAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        var collection = GetTenantCollectionName(tenantSlug);

        var resp = await _http.GetAsync($"/collections/{Uri.EscapeDataString(collection)}", cancellationToken);
        if (resp.IsSuccessStatusCode)
        {
            return;
        }

        // Create collection
        var createBody = new
        {
            vectors = new
            {
                size = DefaultVectorSize,
                distance = "Cosine"
            }
        };

        var createResp = await _http.PutAsJsonAsync($"/collections/{Uri.EscapeDataString(collection)}", createBody, cancellationToken);
        createResp.EnsureSuccessStatusCode();
    }

    public async Task<string> SaveAnalysisAsync(
        string tenantSlug,
        string analysisText,
        string analysisType = "analysis",
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(analysisText))
        {
            throw new ArgumentException("analysisText is required", nameof(analysisText));
        }

        await EnsureTenantCollectionAsync(tenantSlug, cancellationToken);

        // NOTE: Embedding generation is handled by the Python MCP side.
        // Here we store payload-only if vectors are already produced elsewhere.
        // To keep Qdrant schema valid, we upsert a zero-vector as placeholder.
        var vector = new float[DefaultVectorSize];

        var id = CreateStableId($"{tenantSlug}:{analysisType}:{analysisText}");

        var payload = new Dictionary<string, object?>
        {
            ["_tenant"] = tenantSlug,
            ["type"] = "analysis",
            ["analysis_type"] = analysisType,
            ["created_at"] = DateTime.UtcNow.ToString("O"),
            ["text"] = analysisText
        };

        if (metadata is not null)
        {
            foreach (var kv in metadata)
            {
                payload[kv.Key] = kv.Value;
            }
        }

        var upsertBody = new
        {
            points = new[]
            {
                new
                {
                    id,
                    vector,
                    payload
                }
            }
        };

        var collection = GetTenantCollectionName(tenantSlug);
        var upsertResp = await _http.PutAsJsonAsync($"/collections/{Uri.EscapeDataString(collection)}/points?wait=true", upsertBody, cancellationToken);
        upsertResp.EnsureSuccessStatusCode();

        return id;
    }

    public async Task<IReadOnlyList<AnalysisSearchResult>> SearchAnalysisAsync(
        string tenantSlug,
        string queryText,
        int limit = 5,
        double scoreThreshold = 0.5,
        string? filterType = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return Array.Empty<AnalysisSearchResult>();
        }

        await EnsureTenantCollectionAsync(tenantSlug, cancellationToken);

        // Without embeddings, meaningful semantic search cannot happen.
        // This method is provided for completeness, but in practice the MCP server
        // should perform vector search and return results.
        // We still implement a Qdrant "scroll" fallback to retrieve recent payloads.

        var must = new List<object>
        {
            new
            {
                key = "_tenant",
                match = new { value = tenantSlug }
            }
        };

        if (!string.IsNullOrWhiteSpace(filterType))
        {
            must.Add(new
            {
                key = "analysis_type",
                match = new { value = filterType }
            });
        }

        var body = new
        {
            filter = new { must },
            limit = Math.Clamp(limit, 1, 50),
            with_payload = true,
            with_vector = false
        };

        var collection = GetTenantCollectionName(tenantSlug);
        var resp = await _http.PostAsJsonAsync($"/collections/{Uri.EscapeDataString(collection)}/points/scroll", body, cancellationToken);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<QdrantScrollResponse>(cancellationToken: cancellationToken);
        var points = json?.Result?.Points ?? new List<QdrantPoint>();

        // Fallback doesn't compute similarity; set Score=1.0 and let caller decide.
        var results = points
            .Select(p => new AnalysisSearchResult
            {
                Score = 1.0,
                Text = p.Payload?.TryGetValue("text", out var t) == true ? t?.ToString() : null,
                AnalysisType = p.Payload?.TryGetValue("analysis_type", out var at) == true ? at?.ToString() : null,
                CreatedAt = TryParseDate(p.Payload),
                Metadata = p.Payload is null
                    ? new Dictionary<string, object>()
                    : p.Payload.ToDictionary(k => k.Key, v => ConvertJsonElement(v.Value) ?? string.Empty)
            })
            .Where(r => r.Score >= scoreThreshold)
            .ToList();

        return results;
    }

    private static DateTime? TryParseDate(Dictionary<string, JsonElement?>? payload)
    {
        if (payload is null)
        {
            return null;
        }

        if (payload.TryGetValue("created_at", out var el) && el.HasValue)
        {
            if (el.Value.ValueKind == JsonValueKind.String && DateTime.TryParse(el.Value.GetString(), out var dt))
            {
                return dt;
            }
        }

        return null;
    }

    private static object? ConvertJsonElement(JsonElement? element)
    {
        if (!element.HasValue)
        {
            return null;
        }

        var el = element.Value;
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.TryGetDouble(out var d) ? d : null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => el.ToString()
        };
    }

    private static string GetTenantCollectionName(string tenantSlug)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            throw new ArgumentException("tenantSlug is required", nameof(tenantSlug));
        }

        return $"tenant_{tenantSlug}";
    }

    private static string CreateStableId(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }

    private sealed class QdrantScrollResponse
    {
        [JsonPropertyName("result")]
        public QdrantScrollResult? Result { get; set; }
    }

    private sealed class QdrantScrollResult
    {
        [JsonPropertyName("points")]
        public List<QdrantPoint> Points { get; set; } = new();
    }

    private sealed class QdrantPoint
    {
        [JsonPropertyName("id")]
        public JsonElement Id { get; set; }

        [JsonPropertyName("payload")]
        public Dictionary<string, JsonElement?>? Payload { get; set; }
    }
}
