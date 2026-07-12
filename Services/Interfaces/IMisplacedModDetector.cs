using System.Threading;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Extracts cross-installation detection heuristics.
/// </summary>
public interface IMisplacedModDetector
{
    /// <summary>
    /// Detects misplaced mods (client mods in server folder and vice-versa) and cross-installed directories.
    /// </summary>
    Task<MisplacedModReport> DetectMisplacedModsAsync(string sptPath, CancellationToken cancellationToken = default);
}
