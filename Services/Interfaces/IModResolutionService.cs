using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// Handles resolving missing API source code URLs by searching the Forge API
/// using GUIDs and fuzzy name matching.
/// </summary>
public interface IModResolutionService
{
    /// <summary>
    /// Fetches source code URLs from the API for mods that have warnings, mutating the mods in place.
    /// </summary>
    Task FetchSourceCodeUrlsForModsAsync(
        List<Mod> mods,
        Version sptVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fetches source code URLs from the API for paired mods with reconciliation warnings.
    /// </summary>
    Task FetchSourceCodeUrlsForPairedModsAsync(
        List<ModPair> pairs,
        Version sptVersion,
        CancellationToken cancellationToken = default
    );
}
