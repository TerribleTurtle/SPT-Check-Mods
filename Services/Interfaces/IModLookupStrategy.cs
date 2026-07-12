using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Strategy for looking up mods in the Forge API using GUIDs and fuzzy name matching.
/// </summary>
/// <remarks>
/// Heuristics applied in order:
/// 1. Exact match on local name (Score: 100).
/// 2. Exact match on local name with component suffixes removed (Score: 100).
/// 3. Exact match on URL slug against local name or GUID-extracted name (Score: 100).
/// 4. Author name verification coupled with exact name match (Score: 100).
/// 5. Fuzzy matching utilizing Levenshtein distance on name and slug. Returns the highest score above a minimum threshold (e.g., <see cref="MatchingConstants.MinimumFuzzyMatchScore"/>).
/// </remarks>
public interface IModLookupStrategy
{
    /// <summary>
    /// Looks up a mod in the API. Returns the best match and its confidence score (1-100).
    /// </summary>
    /// <param name="mod">The mod to look up.</param>
    /// <param name="sptVersion">The SPT version for compatibility filtering.</param>
    /// <param name="additionalGuidsToTry">Optional list of additional GUIDs to try.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A tuple containing <c>Match</c> (the best mod search result) and <c>ConfidenceScore</c> (a score from 1-100 indicating match confidence), or null if no match was found.</returns>
    Task<(ModSearchResult Match, int ConfidenceScore)?> LookupModAsync(
        Mod mod,
        Version sptVersion,
        IReadOnlyList<string>? additionalGuidsToTry = null,
        CancellationToken cancellationToken = default
    );
}
