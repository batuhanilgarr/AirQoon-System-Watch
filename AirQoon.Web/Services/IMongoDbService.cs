using AirQoon.Web.Services.MongoModels;

namespace AirQoon.Web.Services;

public interface IMongoDbService
{
    Task<IReadOnlyList<TenantInfo>> GetTenantsAsync(int limit = 200, CancellationToken cancellationToken = default);

    Task<TenantInfo?> GetTenantBySlugAsync(string tenantSlug, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceInfoRecord>> GetDevicesByTenantSlugAsync(string tenantSlug, int limit = 200, CancellationToken cancellationToken = default);

    Task<bool> TenantExistsAsync(string tenantSlug, CancellationToken cancellationToken = default);
}
