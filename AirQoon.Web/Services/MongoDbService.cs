using AirQoon.Web.Services.MongoModels;
using MongoDB.Driver;
using Microsoft.Extensions.Caching.Memory;

namespace AirQoon.Web.Services;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoCollection<TenantInfo> _tenants;
    private readonly IMongoCollection<DeviceInfoRecord> _devices;
    private readonly IMemoryCache _cache;

    public MongoDbService(IMongoClient mongoClient, IConfiguration configuration, IMemoryCache cache)
    {
        _cache = cache;
        var databaseName = configuration["Mongo:Database"];
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Mongo:Database configuration is missing.");
        }

        var db = mongoClient.GetDatabase(databaseName);
        _tenants = db.GetCollection<TenantInfo>("Tenants");
        _devices = db.GetCollection<DeviceInfoRecord>("Devices");
    }

    public async Task<IReadOnlyList<TenantInfo>> GetTenantsAsync(int limit = 200, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 1000);

        var cacheKey = $"mongo:tenants:{limit}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<TenantInfo>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _tenants
            .Find(Builders<TenantInfo>.Filter.Empty)
            .SortBy(x => x.Name)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(2) });
        return result;
    }

    public async Task<IReadOnlyList<DeviceInfoRecord>> GetDevicesAsync(int limit = 1000, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 5000);

        var cacheKey = $"mongo:devices:all:{limit}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<DeviceInfoRecord>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _devices
            .Find(Builders<DeviceInfoRecord>.Filter.Empty)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(2) });
        return result;
    }

    public async Task<TenantInfo?> GetTenantBySlugAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return null;
        }

        return await _tenants
            .Find(x => x.SlugName == tenantSlug)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceInfoRecord>> GetDevicesByTenantSlugAsync(string tenantSlug, int limit = 200, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return Array.Empty<DeviceInfoRecord>();
        }

        limit = Math.Clamp(limit, 1, 1000);

        var cacheKey = $"mongo:devices:{tenantSlug}:{limit}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<DeviceInfoRecord>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await _devices
            .Find(x => x.TenantSlugName == tenantSlug)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(2) });
        return result;
    }

    public async Task<bool> TenantExistsAsync(string tenantSlug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
        {
            return false;
        }

        var cacheKey = $"mongo:tenant-exists:{tenantSlug}";
        if (_cache.TryGetValue(cacheKey, out bool cached))
        {
            return cached;
        }

        var count = await _tenants
            .CountDocumentsAsync(x => x.SlugName == tenantSlug, cancellationToken: cancellationToken);

        var exists = count > 0;
        _cache.Set(cacheKey, exists, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
        return exists;
    }
}
