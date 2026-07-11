using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModReconciliationService"/>.
/// </summary>
public sealed class FakeModReconciliationService : IModReconciliationService
{
    /// <summary> Gets or sets the result to return. </summary>
    public ModReconciliationResult ResultToReturn { get; set; } =
        new ModReconciliationResult
        {
            Mods = [],
            ReconciledPairs = [],
            UnmatchedServerMods = [],
            UnmatchedClientMods = [],
        };

    /// <inheritdoc />
    public ModReconciliationResult ReconcileMods(List<Mod> serverMods, List<Mod> clientMods)
    {
        return ResultToReturn;
    }
}

