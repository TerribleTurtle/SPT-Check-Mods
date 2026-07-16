using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OneOf;

namespace CheckModsExtended.Services.Decorators;

/// <summary>
/// A decorator for ISptVersionClient that caches method results.
/// </summary>
public sealed class CachedSptVersionClient(
    ISptVersionClient inner,
    IMemoryCache cache,
    ILogger<CachedSptVersionClient> logger
) : ISptVersionClient
{
    private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    };

    private async Task<T> GetCachedAsync<T>(string key, Func<Task<T>> factory)
        where T : IOneOf
    {
        if (cache.TryGetValue(key, out T? cachedValue) && cachedValue is not null)
        {
            logger.LogDebug("Cache hit for {Key}", key);
            return cachedValue;
        }

        var result = await factory();

        if (result.Value is not ApiError)
        {
            cache.Set(key, result, _cacheEntryOptions);
        }

        return result;
    }

    /// <inheritdoc />
    public Task<OneOf<bool, InvalidSptVersion, ApiError>> ValidateSptVersionAsync(
        string sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var key = $"ForgeApi_ValidateSptVersion_{sptVersion}";
        return GetCachedAsync(key, () => inner.ValidateSptVersionAsync(sptVersion, cancellationToken));
    }

    /// <inheritdoc />
    public Task<OneOf<List<SptVersionResult>, ApiError>> GetAllSptVersionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var key = "ForgeApi_GetAllSptVersions";
        return GetCachedAsync(key, () => inner.GetAllSptVersionsAsync(cancellationToken));
    }
}
