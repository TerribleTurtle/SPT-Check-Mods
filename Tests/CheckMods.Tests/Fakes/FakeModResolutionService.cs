using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

public sealed class FakeModResolutionService : IModResolutionService
{
    public bool FetchSourceCodeUrlsForModsCalled { get; private set; }
    public bool FetchSourceCodeUrlsForPairedModsCalled { get; private set; }

    public Task<IReadOnlyList<Mod>> FetchSourceCodeUrlsForModsAsync(
        IEnumerable<Mod> mods,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        FetchSourceCodeUrlsForModsCalled = true;
        return Task.FromResult<IReadOnlyList<Mod>>(mods.ToList());
    }

    public Task<(IReadOnlyList<ModPair> UpdatedPairs, IReadOnlyList<Mod> UpdatedMods)> FetchSourceCodeUrlsForPairedModsAsync(
        IEnumerable<ModPair> pairs,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        FetchSourceCodeUrlsForPairedModsCalled = true;
        return Task.FromResult<(IReadOnlyList<ModPair>, IReadOnlyList<Mod>)>((pairs.ToList(), []));
    }
}
