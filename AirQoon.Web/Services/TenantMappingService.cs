using AirQoon.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AirQoon.Web.Services;

public class TenantMappingService : ITenantMappingService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public TenantMappingService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<string?> GetTenantSlugForDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var normalized = domain.Trim().ToLowerInvariant();

        var cacheKey = $"domain-tenant:{normalized}";
        if (_cache.TryGetValue(cacheKey, out string? cached))
        {
            return cached;
        }

        var mapping = await _db.DomainTenantMappings
            .AsNoTracking()
            .Where(x => x.IsActive && x.Domain.ToLower() == normalized)
            .Select(x => x.TenantSlug)
            .FirstOrDefaultAsync(cancellationToken);

        _cache.Set(cacheKey, mapping, new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(5)
        });

        return mapping;
    }
}
