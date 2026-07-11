using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

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

    public Task<List<Mod>> ProcessClientDllsInParallelAsync(List<string> dllFiles, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ProcessedClientMods.ToList());
    }

    public List<Mod> ConsolidateDirectoryMods(string directory, List<string> dllPaths)
    {
        return ConsolidatedMods.ToList();
    }

    public Mod? TryDetectClientMod(string dllPath)
    {
        return DetectedClientMod;
    }

    public List<PluginDll> ReadPluginDlls(List<string> dllPaths)
    {
        return PluginDlls.ToList();
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
