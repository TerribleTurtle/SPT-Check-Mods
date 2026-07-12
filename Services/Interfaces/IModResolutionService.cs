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
    /// <param name="mods">The mods to fetch source code URLs for.</param>
    /// <param name="sptVersion">The SPT version for compatibility filtering.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The same list of mods, updated in place with source code URLs where found.</returns>
    Task<IReadOnlyList<Mod>> FetchSourceCodeUrlsForModsAsync(
        IEnumerable<Mod> mods,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fetches source code URLs from the API for paired mods with reconciliation warnings.
    /// </summary>
    /// <param name="pairs">The mod pairs to fetch source code URLs for.</param>
    /// <param name="sptVersion">The SPT version for compatibility filtering.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A tuple containing <c>UpdatedPairs</c> (the updated mod pairs) and <c>UpdatedMods</c> (the updated individual mods).</returns>
    Task<(IReadOnlyList<ModPair> UpdatedPairs, IReadOnlyList<Mod> UpdatedMods)> FetchSourceCodeUrlsForPairedModsAsync(
        IEnumerable<ModPair> pairs,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );
}
