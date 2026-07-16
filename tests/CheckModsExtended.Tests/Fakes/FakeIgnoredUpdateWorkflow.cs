using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeIgnoredUpdateWorkflow : IIgnoredUpdateWorkflow
{
    public EndOfRunChoice ChoiceToReturn { get; set; } = EndOfRunChoice.Exit;
    public IReadOnlyList<Mod>? LastModsProvided { get; private set; }

    public Task<EndOfRunChoice> RunAsync(IReadOnlyList<Mod>? mods, CancellationToken cancellationToken = default)
    {
        LastModsProvided = mods;
        return Task.FromResult(ChoiceToReturn);
    }
}
