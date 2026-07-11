namespace CheckMods.Models;

/// <summary>
/// Represents a mod match that requires user confirmation due to a low confidence score. Used to queue up matches for
/// interactive confirmation during the verification process.
/// </summary>
/// <param name="OriginalMod">The original mod package before any name transformations.</param>
/// <param name="ApiMatch">The matched mod from the Forge API search results.</param>
/// <param name="ConfidenceScore">The confidence score of the match (0-100).</param>
/// <param name="ResultIndex">The index of the result in the processing queue.</param>
public sealed class PendingConfirmation(
    Mod originalMod,
    ModSearchResult apiMatch,
    int confidenceScore
)
{
    /// <summary>
    /// The original mod package information before any name transformations or updates.
    /// </summary>
    public Mod OriginalMod { get; set; } = originalMod;

    /// <summary>
    /// The matched mod from the Forge API search results.
    /// </summary>
    public ModSearchResult ApiMatch { get; set; } = apiMatch;

    /// <summary>
    /// The confidence score of the match based on fuzzy string matching (0-100).
    /// </summary>
    public int ConfidenceScore { get; set; } = confidenceScore;

    /// <summary>
    /// The index of this result in the processing queue for display purposes.
    /// </summary>
    public int ResultIndex { get; set; }
}
