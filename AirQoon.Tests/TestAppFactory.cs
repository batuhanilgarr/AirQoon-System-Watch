using AirQoon.Web.Data;
using AirQoon.Web.Models.Dtos;
using AirQoon.Web.Services;
using AirQoon.Web.Services.MongoModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AirQoon.Tests;

public class TestAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace DbContext with InMemory
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDescriptor is not null)
            {
                services.Remove(dbDescriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("airqoon-tests"));

            // Replace external services with fakes
            services.AddScoped<IMongoDbService, FakeMongoDbService>();
            services.AddScoped<IPostgresAirQualityService, FakePostgresAirQualityService>();
            services.AddScoped<IAirQualityMcpService, FakeAirQualityMcpService>();
            services.AddScoped<ITenantMappingService, FakeTenantMappingService>();
        });
    }
}

internal sealed class FakeMongoDbService : IMongoDbService
{
    public Task<IReadOnlyList<TenantInfo>> GetTenantsAsync(int limit = 200, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TenantInfo>>(new List<TenantInfo>
        {
            new() { SlugName = "akcansa", Name = "Akçansa", IsPublic = false }
        });
    }

    public Task<TenantInfo?> GetTenantBySlugAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        if (tenantSlug == "akcansa")
        {
            return Task.FromResult<TenantInfo?>(new TenantInfo { SlugName = "akcansa", Name = "Akçansa", IsPublic = false });
        }

        return Task.FromResult<TenantInfo?>(null);
    }

    public Task<IReadOnlyList<DeviceInfoRecord>> GetDevicesByTenantSlugAsync(string tenantSlug, int limit = 200, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<DeviceInfoRecord>>(new List<DeviceInfoRecord>
        {
            new() { DeviceId = "dev-1", TenantSlugName = tenantSlug, Name = "D1", Label = "L1" }
        });
    }

    public Task<bool> TenantExistsAsync(string tenantSlug, CancellationToken cancellationToken = default)
        => Task.FromResult(tenantSlug == "akcansa");
}

internal sealed class FakePostgresAirQualityService : IPostgresAirQualityService
{
    public IReadOnlyList<string> NormalizePollutants(IEnumerable<string> pollutants)
        => pollutants.Select(LlmService.NormalizePollutantDbParameter).ToList();

    public Task<IReadOnlyList<AirQualityAggregate>> GetAggregatesAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        IReadOnlyList<string> pollutants,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AirQualityAggregate>>(new List<AirQualityAggregate>
        {
            new() { Parameter = LlmService.NormalizePollutantDbParameter(pollutants.First()), Average = 10, Minimum = 1, Maximum = 20, MeasurementCount = 100, Unit = "µg/m³" }
        });
    }
}

internal sealed class FakeAirQualityMcpService : IAirQualityMcpService
{
    public Task<TimeRangeAnalysisResult> TenantTimeRangeAnalysisAsync(string tenantSlug, DateTime startDate, DateTime endDate, List<string>? pollutants = null, DateTime? comparisonStartDate = null, DateTime? comparisonEndDate = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new TimeRangeAnalysisResult { TenantSlug = tenantSlug, RawText = "# fake time range analysis" });

    public Task<MonthlyComparisonResult> TenantMonthlyComparisonAsync(string tenantSlug, string month1, string month2, int? year = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new MonthlyComparisonResult { TenantSlug = tenantSlug, RawText = "# fake monthly comparison" });

    public Task<IReadOnlyList<DeviceInfo>> GetTenantDevicesAsync(string tenantSlug, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DeviceInfo>>(new List<DeviceInfo>());

    public Task<TenantStatistics> GetTenantStatisticsAsync(string tenantSlug, CancellationToken cancellationToken = default)
        => Task.FromResult(new TenantStatistics { TenantSlug = tenantSlug, DeviceCount = 1, VectorPoints = 0, IsPublic = false, RawText = "# fake stats" });

    public Task<string> SaveAnalysisToVectorDbAsync(string tenantSlug, string analysisText, string analysisType = "analysis", Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        => Task.FromResult("vid-1");

    public Task<IReadOnlyList<AnalysisSearchResult>> SearchAnalysisFromVectorDbAsync(string tenantSlug, string queryText, int limit = 5, double scoreThreshold = 0.5, string? filterType = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AnalysisSearchResult>>(new List<AnalysisSearchResult> { new() { Score = 0.9, Text = "fake rag result" } });
}

internal sealed class FakeTenantMappingService : ITenantMappingService
{
    public Task<string?> GetTenantSlugForDomainAsync(string domain, CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(null);
}
