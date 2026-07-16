using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

public interface IPluginScanCache
{
    void AddPlugins(string directory, IReadOnlyList<PluginDll> plugins);
    bool TryGetPlugins(string directory, out IReadOnlyList<PluginDll>? plugins);
    void Clear();
}
