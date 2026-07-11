using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Singleton)]
public sealed class PluginScanCache : IPluginScanCache
{
    private readonly ConcurrentDictionary<string, IReadOnlyList<PluginDll>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public void AddPlugins(string directory, IReadOnlyList<PluginDll> plugins)
    {
        _cache[directory] = plugins;
    }

    public bool TryGetPlugins(string directory, out IReadOnlyList<PluginDll>? plugins)
    {
        return _cache.TryGetValue(directory, out plugins);
    }

    public void Clear()
    {
        _cache.Clear();
    }
}

