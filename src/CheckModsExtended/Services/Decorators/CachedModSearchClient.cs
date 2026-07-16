using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OneOf;
using SemanticVersioning;

namespace CheckModsExtended.Services.Decorators;

/// <summary>
/// A decorator for IModSearchClient that caches method results.
/// </summary>
public sealed class CachedModSearchClient(
    IModSearchClient inner,
    IMemoryCache cache,
    ILogger<CachedModSearchClient> logger
) : IModSearchClient
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
    public Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var key = $"ForgeApi_SearchMods_{modName}_{sptVersion}";
        return GetCachedAsync(key, () => inner.SearchModsAsync(modName, sptVersion, cancellationToken));
    }

    /// <inheritdoc />
    public Task<OneOf<List<ModSearchResult>, ApiError>> SearchClientModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var key = $"ForgeApi_SearchClientMods_{modName}_{sptVersion}";
        return GetCachedAsync(key, () => inner.SearchClientModsAsync(modName, sptVersion, cancellationToken));
    }

    /// <inheritdoc />
    public Task<OneOf<ModSearchResult, NotFound, InvalidInput, ApiError>> GetModByIdAsync(
        int modId,
        CancellationToken cancellationToken = default
    )
    {
        var key = $"ForgeApi_GetModById_{modId}";
        return GetCachedAsync(key, () => inner.GetModByIdAsync(modId, cancellationToken));
    }

    /// <inheritdoc />
    public Task<OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError>> GetModByGuidAsync(
        string modGuid,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var key = $"ForgeApi_GetModByGuid_{modGuid}_{sptVersion}";
        return GetCachedAsync(key, () => inner.GetModByGuidAsync(modGuid, sptVersion, cancellationToken));
    }
}
