using System.Net.Http.Json;
using System.Text.Json;

namespace AirQoon.Web.Services;

public class McpClientService : IMcpClientService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public McpClientService(HttpClient http, IConfiguration configuration)
    {
        _http = http;
        var baseUrl = configuration["Mcp:HttpBaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl) && _http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(baseUrl);
        }

        if (_http.BaseAddress is null)
        {
            throw new InvalidOperationException("Mcp:HttpBaseUrl is missing (or HttpClient BaseAddress not set).");
        }
    }

    public async Task<T> CallToolAsync<T>(string toolName, object arguments, CancellationToken cancellationToken = default)
    {
        var text = await CallToolAsync(toolName, arguments, cancellationToken);

        // Many MCP tools return markdown/plain-text; allow DTOs to capture RawText.
        // If T is string, return text.
        if (typeof(T) == typeof(string))
        {
            return (T)(object)text;
        }

        // Try JSON deserialize if tool happens to return JSON
        try
        {
            var obj = JsonSerializer.Deserialize<T>(text, JsonOptions);
            if (obj is not null)
            {
                return obj;
            }
        }
        catch
        {
            // ignored
        }

        // If DTO has RawText property, populate it.
        var instance = Activator.CreateInstance<T>();
        var prop = typeof(T).GetProperty("RawText");
        if (prop is not null && prop.CanWrite && prop.PropertyType == typeof(string))
        {
            prop.SetValue(instance, text);
        }

        return instance;
    }

    public async Task<string> CallToolAsync(string toolName, object arguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("toolName is required", nameof(toolName));
        }

        var body = new
        {
            tool = toolName,
            arguments
        };

        var resp = await _http.PostAsJsonAsync("/call_tool", body, cancellationToken);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<McpCallToolResponse>(cancellationToken: cancellationToken);
        return json?.text ?? string.Empty;
    }

    private sealed class McpCallToolResponse
    {
        public string? text { get; set; }
    }
}
