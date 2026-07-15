using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Singleton)]
public sealed class CacheManager(IMemoryCache memoryCache) : ICacheManager
{
    public void Clear()
    {
        if (memoryCache is MemoryCache concreteCache)
        {
            concreteCache.Clear();
        }
    }
}
