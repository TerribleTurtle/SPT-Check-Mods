using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class CacheManager : ICacheManager
{
    private readonly IMemoryCache _memoryCache;

    public CacheManager(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public void Clear()
    {
        if (_memoryCache is MemoryCache concreteCache)
        {
            concreteCache.Clear();
        }
    }
}
