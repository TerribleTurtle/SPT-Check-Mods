using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// UI Renderer for displaying early pipeline warnings such as reconciliation and unverified mods.
/// </summary>
public interface IReconciliationUiRenderer
{
    /// <summary>
    /// Renders the results of the server/client mod reconciliation process.
    /// </summary>
    /// <param name="result">The reconciliation result.</param>
    void ReconciliationResults(ModReconciliationResult result);

    /// <summary>
    /// Renders load warnings for mods that failed certain checks.
    /// </summary>
    /// <param name="modsWithWarnings">List of mods with load warnings.</param>
    void LoadingWarnings(List<Mod> modsWithWarnings);

    /// <summary>
    /// Renders a list of mods that were not successfully matched to the Forge API.
    /// </summary>
    /// <param name="mods">List of unmatched mods.</param>
    void UnverifiedMods(List<Mod> mods);
}
