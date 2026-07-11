using System.Collections.Generic;
using CheckMods.Models;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// Handles checking if mods are compatible with the currently installed SPT version.
/// </summary>
public interface ICompatibilityValidationService
{
    /// <summary>
    /// Checks mod version compatibility with the installed SPT version, 
    /// flagging any incompatibilities.
    /// </summary>
    void CheckModVersionCompatibility(List<Mod> mods, Version sptVersion);
}
