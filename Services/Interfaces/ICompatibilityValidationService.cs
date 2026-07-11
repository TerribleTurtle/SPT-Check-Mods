using System.Collections.Generic;
using CheckModsExtended.Models;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles checking if mods are compatible with the currently installed SPT version.
/// </summary>
public interface ICompatibilityValidationService
{
    /// <summary>
    /// Checks mod version compatibility with the installed SPT version,
    /// flagging any incompatibilities.
    /// </summary>
    (IReadOnlyList<Mod> UpdatedMods, IReadOnlyList<string> ValidationEvents) CheckModVersionCompatibility(IEnumerable<Mod> mods, SemanticVersioning.Version sptVersion);
}
