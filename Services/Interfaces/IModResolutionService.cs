using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles resolving missing API source code URLs by searching the Forge API
/// using GUIDs and fuzzy name matching.
/// </summary>
public interface IModResolutionService
{
    /// <summary>
    /// Fetches source code URLs from the API for mods that have warnings, mutating the mods in place.
    /// </summary>
    Task<IReadOnlyList<Mod>> FetchSourceCodeUrlsForModsAsync(
        IEnumerable<Mod> mods,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fetches source code URLs from the API for paired mods with reconciliation warnings.
    /// </summary>
    Task<(IReadOnlyList<ModPair> UpdatedPairs, IReadOnlyList<Mod> UpdatedMods)> FetchSourceCodeUrlsForPairedModsAsync(
        IEnumerable<ModPair> pairs,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );
}

