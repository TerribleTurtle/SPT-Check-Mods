using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeUpdateOrchestrationService : IUpdateOrchestrationService
{
    public bool CheckForSptUpdatesCalled { get; private set; }
    public bool CheckForCheckModsExtendedUpdateCalled { get; private set; }
    public bool ApplyIgnoredUpdatesCalled { get; private set; }

    public IReadOnlyList<Mod> ModsToReturn { get; set; } = [];

    public Task CheckForSptUpdatesAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        CheckForSptUpdatesCalled = true;
        return Task.CompletedTask;
    }

    public Task CheckForCheckModsExtendedUpdateAsync(Version sptVersion, CancellationToken cancellationToken = default)
    {
        CheckForCheckModsExtendedUpdateCalled = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Mod>> ApplyIgnoredUpdatesAsync(
        IEnumerable<Mod> mods,
        CancellationToken cancellationToken = default
    )
    {
        ApplyIgnoredUpdatesCalled = true;
        if (ModsToReturn.Count > 0)
        {
            return Task.FromResult(ModsToReturn);
        }
        return Task.FromResult<IReadOnlyList<Mod>>(mods.ToList());
    }
}
