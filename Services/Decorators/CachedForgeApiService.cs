using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OneOf;

namespace CheckModsExtended.Services.Decorators;

/// <summary>
/// A decorator for IForgeApiService that caches method results.
/// </summary>
public sealed class CachedForgeApiService(
    IForgeApiService inner,
    IMemoryCache cache,
    ILogger<CachedForgeApiService> logger
) : IForgeApiService
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

    /// <inheritdoc />
    public Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesAsync(
        IEnumerable<(int ModId, string CurrentVersion)> modUpdates,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var updatesList = modUpdates.ToList();
        var updatesKey = string.Join(",", updatesList.Select(m => $"{m.ModId}_{m.CurrentVersion}").OrderBy(x => x));
        var key = $"ForgeApi_GetModUpdates_{updatesKey}_{sptVersion}";

        return GetCachedAsync(key, () => inner.GetModUpdatesAsync(updatesList, sptVersion, cancellationToken));
    }

    /// <inheritdoc />
    public Task<OneOf<List<ModDependency>, NotFound, ApiError>> GetModDependenciesAsync(
        IEnumerable<(string Identifier, string Version)> modVersions,
        CancellationToken cancellationToken = default
    )
    {
        var versionsList = modVersions.ToList();
        var versionsKey = string.Join(",", versionsList.Select(m => $"{m.Identifier}_{m.Version}").OrderBy(x => x));
        var key = $"ForgeApi_GetModDependencies_{versionsKey}";

        return GetCachedAsync(key, () => inner.GetModDependenciesAsync(versionsList, cancellationToken));
    }
}

