using CheckMods.Models;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CheckMods.Services.Decorators;

/// <summary>
/// A decorator for IModDependencyService that caches dependency analysis results.
/// </summary>
public sealed class CachedModDependencyService(
    IModDependencyService inner,
    IMemoryCache cache,
    ILogger<CachedModDependencyService> logger
) : IModDependencyService
{
    private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
    };

    /// <inheritdoc />
    public async Task<DependencyAnalysisResult> AnalyzeDependenciesAsync(
        IEnumerable<Mod> mods,
        HashSet<string> installedModGuids,
        Action<int, int>? progressCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        // Sort items for deterministic cache key
        var modKey = string.Join(",", mods.Select(m => $"{m.Local.Guid}_{m.Local.LocalVersion}").OrderBy(x => x));
        var installedKey = string.Join(",", installedModGuids.OrderBy(x => x));
        var key = $"ModDeps_{modKey}_{installedKey}";

        if (cache.TryGetValue(key, out DependencyAnalysisResult? cachedValue) && cachedValue is not null)
        {
            logger.LogDebug("Cache hit for dependency analysis");
            progressCallback?.Invoke(1, 1);
            return cachedValue;
        }

        logger.LogDebug("Cache miss for dependency analysis");
        var result = await inner.AnalyzeDependenciesAsync(mods, installedModGuids, progressCallback, cancellationToken);
        
        cache.Set(key, result, _cacheEntryOptions);
        
        return result;
    }
}

