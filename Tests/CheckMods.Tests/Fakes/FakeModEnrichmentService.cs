using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModEnrichmentService"/>.
/// </summary>
public sealed class FakeModEnrichmentService : IModEnrichmentService
{
    /// <summary> Gets or sets if service was called. </summary>
    public bool WasCalled { get; set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<Mod>> EnrichAllWithVersionDataAsync(
        IEnumerable<Mod> mods,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        WasCalled = true;
        return Task.FromResult<IReadOnlyList<Mod>>(mods.ToList());
    }
}
