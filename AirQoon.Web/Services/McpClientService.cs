using System.Net.Http.Json;
using System.Text.Json;

namespace AirQoon.Web.Services;

public class McpClientService : IMcpClientService
{
    private readonly HttpClient _http;
    private readonly ILogger<McpClientService> _logger;
    private readonly string _baseUrl;
    private readonly int _timeoutSeconds;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public McpClientService(HttpClient http, IConfiguration configuration, ILogger<McpClientService> logger)
    {
        _http = http;
        _logger = logger;
        _baseUrl = configuration["Mcp:HttpBaseUrl"] ?? "http://localhost:5005";
        _timeoutSeconds = configuration.GetValue<int>("Mcp:TimeoutSeconds", 30);
        
        if (_http.BaseAddress is null)
        {
            _http.BaseAddress = new Uri(_baseUrl);
        }

        _http.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
        
        _logger.LogInformation("MCP Client initialized with BaseUrl={BaseUrl}, Timeout={TimeoutSeconds}s", _baseUrl, _timeoutSeconds);
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

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            
            var response = await _http.GetAsync("/healthz", cts.Token);
            var isHealthy = response.IsSuccessStatusCode;
            
            if (!isHealthy)
            {
                _logger.LogWarning("MCP health check failed: StatusCode={StatusCode}", response.StatusCode);
            }
            
            return isHealthy;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "MCP health check failed: Connection error to {BaseUrl}", _baseUrl);
            return false;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("MCP health check timeout after 5 seconds");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP health check failed: Unexpected error");
            return false;
        }
    }

    public async Task<string> CallToolAsync(string toolName, object arguments, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException("toolName is required", nameof(toolName));
        }

        try
        {
            // Fast pre-flight to avoid waiting full tool timeout when MCP is down/unready.
            if (!await IsHealthyAsync(cancellationToken))
            {
                throw new InvalidOperationException($"MCP servisi şu anda kullanılamıyor: {_baseUrl}. MCP sunucusunun çalıştığından emin olun.");
            }

            var body = new
            {
                tool = toolName,
                arguments
            };

            _logger.LogDebug("Calling MCP tool: {ToolName}", toolName);
            
            var resp = await _http.PostAsJsonAsync("/call_tool", body, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<McpCallToolResponse>(cancellationToken: cancellationToken);
            return json?.text ?? string.Empty;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Connection refused") || ex.InnerException?.Message.Contains("Connection refused") == true)
        {
            _logger.LogError(ex, "MCP service connection refused: {BaseUrl}. Ensure MCP server is running.", _baseUrl);
            throw new InvalidOperationException($"MCP servisi bağlantı hatası: {_baseUrl}. MCP sunucusunun çalıştığından emin olun.", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "MCP service timeout after {TimeoutSeconds}s for tool: {ToolName}", _timeoutSeconds, toolName);
            throw new TimeoutException($"MCP servisi {_timeoutSeconds} saniye içinde yanıt vermedi.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "MCP service HTTP error for tool: {ToolName}", toolName);
            throw new InvalidOperationException($"MCP servisi HTTP hatası: {ex.Message}", ex);
        }
    }

    private sealed class McpCallToolResponse
    {
        public string? text { get; set; }
    }
}
