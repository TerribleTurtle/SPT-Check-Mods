using System.Threading;
using CheckMods.Models;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// Extracts cross-installation detection heuristics.
/// </summary>
public interface IMisplacedModDetector
{
    /// <summary>
    /// Detects misplaced mods (client mods in server folder and vice-versa) and cross-installed directories.
    /// </summary>
    MisplacedModReport DetectMisplacedMods(string sptPath, CancellationToken cancellationToken = default);
}
