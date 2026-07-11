using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

public sealed class FakeUpdateOrchestrationService : IUpdateOrchestrationService
{
    public bool CheckForSptUpdatesCalled { get; private set; }
    public bool CheckForCheckModsUpdateCalled { get; private set; }
    public bool ApplyIgnoredUpdatesCalled { get; private set; }

    public Task CheckForSptUpdatesAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        CheckForSptUpdatesCalled = true;
        return Task.CompletedTask;
    }

    public Task CheckForCheckModsUpdateAsync(Version sptVersion, CancellationToken cancellationToken = default)
    {
        CheckForCheckModsUpdateCalled = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Mod>> ApplyIgnoredUpdatesAsync(
        IEnumerable<Mod> mods,
        CancellationToken cancellationToken = default
    )
    {
        ApplyIgnoredUpdatesCalled = true;
        return Task.FromResult<IReadOnlyList<Mod>>(mods.ToList());
    }
}
