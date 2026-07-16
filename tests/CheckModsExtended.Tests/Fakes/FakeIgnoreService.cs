using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeIgnoreService : IIgnoreService
{
    public bool ShouldThrow { get; set; } = false;
    public List<IgnoredUpdate> IgnoresToReturn { get; set; } = new();

    public Task<IReadOnlyList<IgnoredUpdate>> GetIgnoresAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldThrow) throw new System.Exception("Simulated GetIgnoresAsync exception");
        return Task.FromResult<IReadOnlyList<IgnoredUpdate>>(IgnoresToReturn);
    }

    public Task<bool> AddIgnoreAsync(int apiModId, string localVersion, string ignoredLatestVersion, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow) throw new System.Exception("Simulated AddIgnoreAsync exception");
        return Task.FromResult(true);
    }

    public Task<int> RemoveIgnoreAsync(int apiModId, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow) throw new System.Exception("Simulated RemoveIgnoreAsync exception");
        return Task.FromResult(1);
    }
}
