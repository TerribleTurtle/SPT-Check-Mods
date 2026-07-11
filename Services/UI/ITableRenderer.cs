using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Services.UI;

/// <summary>
/// Renders complex tabular data and tree structures for the CLI.
/// </summary>
public interface ITableRenderer
{
    /// <summary>Displays the SPT version-compatibility results for the checked mods.</summary>
    void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion);

    /// <summary>Displays warnings for the given mods that have loading issues.</summary>
    void LoadingWarnings(List<Mod> modsWithWarnings);

    /// <summary>Displays the results of mod reconciliation.</summary>
    void ReconciliationResults(ModReconciliationResult result);

    /// <summary>Displays mods installed in the wrong location, shown before they're excluded from the remaining checks.</summary>
    void MisplacedMods(MisplacedModReport report);

    /// <summary>Lists mods with no Forge match (informational).</summary>
    void UnverifiedMods(List<Mod> mods);

    /// <summary>Displays the dependency tree and any conflicts or missing dependencies.</summary>
    void DependencyResults(DependencyAnalysisResult result);

    /// <summary>Displays the final version summary table and update/blocked lists.</summary>
    void VersionTable(List<Mod> mods);
}
