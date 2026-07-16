using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Web;
using SemanticVersioning;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// UI Renderer for displaying version tables and compatibility results.
/// </summary>
public interface IVersionTableUiRenderer
{
    /// <summary>
    /// Renders the main mod version summary table and available updates tree.
    /// </summary>
    /// <param name="mods">The list of mods to display.</param>
    void VersionTable(List<Mod> mods);

    /// <summary>
    /// Renders the main mod version summary table from cached DTOs.
    /// </summary>
    /// <param name="mods">The list of cached mods to display.</param>
    void CachedVersionTable(IReadOnlyList<ModDto> mods);

    /// <summary>
    /// Renders the SPT version compatibility results for the given mods.
    /// </summary>
    /// <param name="mods">The list of mods.</param>
    /// <param name="sptVersion">The installed SPT version.</param>
    void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion);
}
