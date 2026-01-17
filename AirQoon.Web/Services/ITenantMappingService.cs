namespace AirQoon.Web.Services;

public interface ITenantMappingService
{
    Task<string?> GetTenantSlugForDomainAsync(string domain, CancellationToken cancellationToken = default);
}
