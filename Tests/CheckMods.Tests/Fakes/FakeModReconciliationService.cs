using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModReconciliationService"/>.
/// </summary>
public sealed class FakeModReconciliationService : IModReconciliationService
{
    /// <summary> Gets or sets the result to return. </summary>
    public ModReconciliationResult ResultToReturn { get; set; } = new ModReconciliationResult
    {
        Mods = [],
        ReconciledPairs = [],
        UnmatchedServerMods = [],
        UnmatchedClientMods = []
    };

    /// <inheritdoc />
    public ModReconciliationResult ReconcileMods(List<Mod> serverMods, List<Mod> clientMods)
    {
        return ResultToReturn;
    }
}
