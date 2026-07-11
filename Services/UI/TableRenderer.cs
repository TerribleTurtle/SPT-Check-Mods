using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.UI;

/// <summary>
/// Spectre.Console implementation of <see cref="ITableRenderer"/>.
/// Functions as a facade that delegates to specialized UI renderers.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TableRenderer(
    IVersionTableUiRenderer versionTableRenderer,
    IReconciliationUiRenderer reconciliationRenderer,
    IMisplacedModUiRenderer misplacedModRenderer,
    IDependencyUiRenderer dependencyRenderer) : ITableRenderer
{
    /// <inheritdoc />
    public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion)
    {
        versionTableRenderer.VersionCompatibilityResults(mods, sptVersion);
    }

    /// <inheritdoc />
    public void LoadingWarnings(List<Mod> modsWithWarnings)
    {
        reconciliationRenderer.LoadingWarnings(modsWithWarnings);
    }

    /// <inheritdoc />
    public void ReconciliationResults(ModReconciliationResult result)
    {
        reconciliationRenderer.ReconciliationResults(result);
    }

    /// <inheritdoc />
    public void MisplacedMods(MisplacedModReport report)
    {
        misplacedModRenderer.MisplacedMods(report);
    }

    /// <inheritdoc />
    public void UnverifiedMods(List<Mod> mods)
    {
        reconciliationRenderer.UnverifiedMods(mods);
    }

    /// <inheritdoc />
    public void DependencyResults(DependencyAnalysisResult result)
    {
        dependencyRenderer.DependencyResults(result);
    }

    /// <inheritdoc />
    public void VersionTable(List<Mod> mods)
    {
        versionTableRenderer.VersionTable(mods);
    }
}


