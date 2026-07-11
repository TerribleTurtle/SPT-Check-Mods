namespace CheckMods.Models;

/// <summary>
/// Represents a mod match that requires user confirmation due to a low confidence score. Used to queue up matches for
/// interactive confirmation during the verification process.
/// </summary>
/// <param name="OriginalMod">The original mod package information before any name transformations or updates.</param>
/// <param name="ApiMatch">The matched mod from the Forge API search results.</param>
/// <param name="ConfidenceScore">The confidence score of the match based on fuzzy string matching (0-100).</param>
/// <param name="ResultIndex">The index of this result in the processing queue for display purposes.</param>
public sealed record PendingConfirmation(
    Mod OriginalMod,
    ModSearchResult ApiMatch,
    int ConfidenceScore,
    int ResultIndex = 0
);