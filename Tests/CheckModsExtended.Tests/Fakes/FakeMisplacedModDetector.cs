using System.Collections.Generic;
using System.Threading;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeMisplacedModDetector : IMisplacedModDetector
{
    public MisplacedModReport ReportToReturn { get; set; } = new MisplacedModReport([], []);

    public Task<MisplacedModReport> DetectMisplacedModsAsync(
        string sptPath,
        System.Collections.Generic.IReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyList<PluginDll>> clientPluginCache,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(ReportToReturn);
    }
}
