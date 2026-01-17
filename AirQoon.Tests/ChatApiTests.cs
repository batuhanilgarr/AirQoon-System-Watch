using System.Net;
using System.Net.Http.Json;
using AirQoon.Web.Models.Chat;
using FluentAssertions;

namespace AirQoon.Tests;

public class ChatApiTests : IClassFixture<TestAppFactory>
{
    private readonly HttpClient _client;

    public ChatApiTests(TestAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_api_chat_should_return_reply_and_rag_append()
    {
        var req = new ChatRequest
        {
            SessionId = Guid.NewGuid().ToString(),
            Message = "akcansa icin 2025-01-01 ile 2025-01-08 arasi PM10 analizi",
            Domain = "local",
            TenantSlug = "akcansa"
        };

        var resp = await _client.PostAsJsonAsync("/api/chat", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<ChatResponse>();
        body.Should().NotBeNull();
        body!.Reply.Should().Contain("akcansa için");
        body.Reply.Should().Contain("İlgili önceki analizler");
        body.TenantSlug.Should().Be("akcansa");
    }

    [Fact]
    public async Task Post_api_chat_comparison_should_use_mcp_fake()
    {
        var req = new ChatRequest
        {
            SessionId = Guid.NewGuid().ToString(),
            Message = "akcansa ocak 2025 ile subat 2025 karsilastir",
            Domain = "local",
            TenantSlug = "akcansa"
        };

        var resp = await _client.PostAsJsonAsync("/api/chat", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<ChatResponse>();
        body!.Reply.Should().Contain("fake monthly comparison");
    }
}
