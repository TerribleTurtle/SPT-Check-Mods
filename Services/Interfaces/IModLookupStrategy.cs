using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// Strategy for looking up mods in the Forge API using GUIDs and fuzzy name matching.
/// </summary>
public interface IModLookupStrategy
{
    /// <summary>
    /// Looks up a mod in the API. Returns the best match and its confidence score (1-100).
    /// </summary>
    Task<(ModSearchResult Match, int ConfidenceScore)?> LookupModAsync(
        Mod mod,
        Version sptVersion,
        IReadOnlyList<string>? additionalGuidsToTry = null,
        CancellationToken cancellationToken = default
    );
}
