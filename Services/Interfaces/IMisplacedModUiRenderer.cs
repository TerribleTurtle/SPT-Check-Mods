using CheckMods.Models;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// UI Renderer for displaying misplaced mod warnings and cross-installed directories.
/// </summary>
public interface IMisplacedModUiRenderer
{
    /// <summary>
    /// Renders warnings for improperly installed mods (server in client folder, etc).
    /// </summary>
    /// <param name="report">The misplaced mod report.</param>
    void MisplacedMods(MisplacedModReport report);
}
