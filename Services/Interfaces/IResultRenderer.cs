using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles rendering complex result tables and lists.
/// </summary>
public interface IResultRenderer
{
    void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion);
    void LoadingWarnings(List<Mod> modsWithWarnings);
    void ReconciliationResults(ModReconciliationResult result);
    void MisplacedMods(MisplacedModReport report);
    void UnverifiedMods(List<Mod> mods);
    void DependencyResults(DependencyAnalysisResult result);
    void VersionTable(List<Mod> mods);
    void PendingConfirmationsSummary(IReadOnlyList<PendingConfirmation> pendingConfirmations);
    void IgnoredUpdatesList(IReadOnlyList<IgnoredUpdate> ignores);
    void InstalledModsList(IReadOnlyList<Mod> serverMods, IReadOnlyList<Mod> clientMods);
}
