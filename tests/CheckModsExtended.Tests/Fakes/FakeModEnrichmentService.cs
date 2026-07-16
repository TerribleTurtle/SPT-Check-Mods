using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SemanticVersioning;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModEnrichmentService"/>.
/// </summary>
public sealed class FakeModEnrichmentService : IModEnrichmentService
{
    /// <summary> Gets or sets if service was called. </summary>
    public bool WasCalled { get; set; }

    public IReadOnlyList<Mod> EnrichedModsToReturn { get; set; } = [];

    /// <inheritdoc />
    public Task<IReadOnlyList<Mod>> EnrichAllWithVersionDataAsync(
        IEnumerable<Mod> mods,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        WasCalled = true;
        if (EnrichedModsToReturn.Count > 0)
        {
            return Task.FromResult(EnrichedModsToReturn);
        }
        return Task.FromResult<IReadOnlyList<Mod>>(mods.ToList());
    }
}
