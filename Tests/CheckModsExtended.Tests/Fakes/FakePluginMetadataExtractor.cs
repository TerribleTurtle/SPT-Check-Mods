using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakePluginMetadataExtractor : IPluginMetadataExtractor
{
    public List<string> ValidClientDllFilesToReturn { get; set; } = [];
    public List<Mod> ProcessedClientMods { get; set; } = [];
    public List<Mod> ConsolidatedMods { get; set; } = [];
    public Mod? DetectedClientMod { get; set; }
    public List<PluginDll> PluginDlls { get; set; } = [];
    public List<List<PluginDll>> PartitionedPlugins { get; set; } = [];
    public MisplacedMod? MisplacedModToReturn { get; set; }

    public List<string> GetValidClientDllFiles(string pluginsPath)
    {
        return ValidClientDllFilesToReturn.ToList();
    }

    public Task<List<Mod>> ProcessClientDllsInParallelAsync(
        List<string> dllFiles,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(ProcessedClientMods.ToList());
    }

    public Task<(List<Mod> Mods, List<PluginDll> Plugins)> ConsolidateDirectoryModsAsync(
        string directory,
        List<string> dllPaths,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult((ConsolidatedMods.ToList(), PluginDlls.ToList()));
    }

    public Task<Mod?> TryDetectClientModAsync(string dllPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(DetectedClientMod);
    }

    public Task<List<PluginDll>> ReadPluginDllsAsync(
        List<string> dllPaths,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(PluginDlls.ToList());
    }

    public List<List<PluginDll>> PartitionByRelatedness(List<PluginDll> plugins)
    {
        return PartitionedPlugins.Select(list => list.ToList()).ToList();
    }

    public MisplacedMod ToMisplacedMod(List<PluginDll> group, string directoryName)
    {
        return MisplacedModToReturn!;
    }
}
