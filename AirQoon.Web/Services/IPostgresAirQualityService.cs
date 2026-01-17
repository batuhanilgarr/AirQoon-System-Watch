using AirQoon.Web.Models.Dtos;

namespace AirQoon.Web.Services;

public interface IPostgresAirQualityService
{
    IReadOnlyList<string> NormalizePollutants(IEnumerable<string> pollutants);

    Task<IReadOnlyList<AirQualityAggregate>> GetAggregatesAsync(
        IReadOnlyList<string> deviceIds,
        DateTime startDateUtc,
        DateTime endDateUtc,
        IReadOnlyList<string> pollutants,
        CancellationToken cancellationToken = default);
}
