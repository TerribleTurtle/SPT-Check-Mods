using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public class FakePluginScanCache : IPluginScanCache
{
    private readonly Dictionary<string, IReadOnlyList<PluginDll>> _cache = new();

    public int ClearCallCount { get; private set; }

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
        ClearCallCount++;
        _cache.Clear();
    }
}
