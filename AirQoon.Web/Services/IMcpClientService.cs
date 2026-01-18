namespace AirQoon.Web.Services;

public interface IMcpClientService
{
    Task<T> CallToolAsync<T>(string toolName, object arguments, CancellationToken cancellationToken = default);

    Task<string> CallToolAsync(string toolName, object arguments, CancellationToken cancellationToken = default);
    
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
