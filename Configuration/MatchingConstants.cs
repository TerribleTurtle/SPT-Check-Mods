namespace CheckMods.Configuration;

/// <summary>
/// Constants used for mod matching operations.
/// </summary>
public static class MatchingConstants
{
    /// <summary>
    /// Minimum fuzzy match score (0-100) required to consider an exact plugin/server match valid.
    /// This was selected as 70 to allow for minor spelling variations or version suffixes
    /// while rejecting completely distinct mods that share common keywords.
    /// </summary>
    public const int MinimumFuzzyMatchScore = 70;

    /// <summary>
    /// The higher fuzzy match score required when falling back to a broad name-only search.
    /// Because name-only searches lack exact plugin GUID or Package.json IDs, a stricter threshold
    /// of 80 is required to prevent aggressive false-positive matches on generic titles.
    /// </summary>
    public const int NameSearchFuzzyThreshold = 80;

    /// <summary>
    /// Maximum length for mod names in display tables before truncation.
    /// </summary>
    public const int MaxDisplayNameLength = 40;

    /// <summary>
    /// Maximum length for author names in display tables before truncation.
    /// </summary>
    public const int MaxDisplayAuthorLength = 20;
}
