using AirQoon.Web.Components;
using AirQoon.Web.Data;
using AirQoon.Web.Data.Entities;
using AirQoon.Web.Services;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration["Mongo:ConnectionString"]));

builder.Services.AddScoped<IMongoDbService, MongoDbService>();

builder.Services.AddScoped<ITenantMappingService, TenantMappingService>();

builder.Services.AddScoped<IPostgresAirQualityService, PostgresAirQualityService>();

builder.Services.AddHttpClient<IVectorDbService, VectorDbService>(client =>
     client.BaseAddress = new Uri(builder.Configuration["Qdrant:Host"]!));

builder.Services.AddHttpClient<IMcpClientService, McpClientService>(client =>
     client.BaseAddress = new Uri(builder.Configuration["Mcp:HttpBaseUrl"]!));

builder.Services.AddScoped<IAirQualityMcpService, AirQualityMcpService>();

builder.Services.AddScoped<IChatOrchestrationService, ChatOrchestrationService>();

builder.Services.AddScoped<ILlmService, LlmService>();

builder.Services.AddScoped<IResponseValidationService, ResponseValidationService>();

builder.Services.AddScoped<IAverageContextService, AverageContextService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    if (!db.AdminSettings.Any())
    {
        db.AdminSettings.Add(new AdminSetting
        {
            Id = 1,
            LlmProvider = "None",
            ModelName = null,
            UpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.MapGet("/debug/mcp/tenant_statistics/{tenantSlug}", async (
    string tenantSlug,
    IAirQualityMcpService mcp,
    CancellationToken cancellationToken) =>
{
    var stats = await mcp.GetTenantStatisticsAsync(tenantSlug, cancellationToken);
    return Results.Ok(stats);
});

app.MapPost("/api/chat", async (
    AirQoon.Web.Models.Chat.ChatRequest req,
    HttpContext http,
    IChatOrchestrationService chat,
    CancellationToken cancellationToken) =>
{
    req.IpAddress ??= http.Connection.RemoteIpAddress?.ToString();
    req.UserAgent ??= http.Request.Headers.UserAgent.ToString();
    var resp = await chat.HandleMessageAsync(req, cancellationToken);
    return Results.Ok(resp);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program
{
}
