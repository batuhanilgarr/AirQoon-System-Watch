using AirQoon.Web.Models.Chat;

namespace AirQoon.Web.Services;

public interface IChatOrchestrationService
{
    Task<ChatResponse> HandleMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
