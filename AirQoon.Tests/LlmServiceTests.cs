using AirQoon.Web.Models.Chat;
using AirQoon.Web.Services;
using FluentAssertions;

namespace AirQoon.Tests;

public class LlmServiceTests
{
    private readonly LlmService _sut = new();

    [Fact]
    public void NormalizePollutantDbParameter_should_map_common_pollutants()
    {
        LlmService.NormalizePollutantDbParameter("PM10").Should().Be("PM10-24h");
        LlmService.NormalizePollutantDbParameter("pm25").Should().Be("PM2.5-24h");
        LlmService.NormalizePollutantDbParameter("NO2").Should().Be("NO2-1h");
        LlmService.NormalizePollutantDbParameter("CO").Should().Be("CO-8h");
    }

    [Fact]
    public void ConvertTenantNameToSlug_should_convert_turkish_chars()
    {
        LlmService.ConvertTenantNameToSlug("Bursa Büyükşehir Belediyesi").Should().Be("bursa-buyuksehir-belediyesi");
        LlmService.ConvertTenantNameToSlug("Akçansa").Should().Be("akcansa");
    }

    [Fact]
    public async Task DetectIntentAsync_should_detect_comparison()
    {
        var r = await _sut.DetectIntentAsync("akcansa ocak 2025 ile subat 2025 karsilastir", null, "akcansa");
        r.Intent.Should().Be(IntentType.ComparisonAnalysis);
        r.Month1.Should().Be("2025-01");
        r.Month2.Should().Be("2025-02");
    }

    [Fact]
    public async Task DetectIntentAsync_should_detect_air_quality_query_and_dates()
    {
        var r = await _sut.DetectIntentAsync("akcansa icin 2025-01-01 ile 2025-01-08 arasi PM10 analizi", null, "akcansa");
        r.Intent.Should().Be(IntentType.AirQualityQuery);
        r.Pollutant.Should().Be("PM10");
        r.StartDateUtc.Should().NotBeNull();
        r.EndDateUtc.Should().NotBeNull();
    }
}
