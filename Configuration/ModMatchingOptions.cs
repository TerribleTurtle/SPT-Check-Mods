namespace CheckModsExtended.Configuration;

/// <summary>
/// Configuration options for mod matching.
/// </summary>
public sealed class ModMatchingOptions
{
    /// <summary>
    /// Minimum number of mods that must all fail before an all-failed batch is treated as a systemic fault
    /// (e.g., to prevent mass false-positives when SPT versions change).
    /// </summary>
    public int MinimumModsForSystemicFailure { get; set; } = 3;
}
