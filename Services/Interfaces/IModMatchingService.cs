using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Service responsible for matching local mods with their Forge API counterparts.
/// </summary>
public interface IModMatchingService
{
    /// <summary>
    /// Matches a single mod with the Forge API using GUID lookup and fuzzy name fallback, updating its metadata in-place.
    /// </summary>
    /// <param name="mod">The mod to match.</param>
    /// <param name="sptVersion">The SPT version for compatibility filtering.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A tuple containing <c>Mod</c> (the updated mod instance) and <c>Confirmation</c> (pending user confirmation if a fuzzy match needs approval, or null).</returns>
    Task<(Mod Mod, PendingConfirmation? Confirmation)> MatchModAsync(
        Mod mod,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Matches multiple mods with the Forge API in parallel, updating each mod's metadata in-place.
    /// </summary>
    /// <param name="mods">The mods to match.</param>
    /// <param name="sptVersion">The SPT version for compatibility filtering.</param>
    /// <param name="progress">Optional callback for progress reporting.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The same mod instances, updated with API match data where found.</returns>
    Task<IReadOnlyList<Mod>> MatchModsAsync(
        IEnumerable<Mod> mods,
        SemanticVersioning.Version sptVersion,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    );
}
