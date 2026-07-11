namespace CheckMods.Models;

/// <summary>
/// Contains metadata extracted locally from the physical mod files installed by the user.
/// </summary>
public sealed record LocalModIdentity
{
    /// <summary>
    /// A unique identifier for the local mod.
    /// </summary>
    public required string Guid { get; init; }

    /// <summary>
    /// The absolute path to the main file defining the mod.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Indicates whether this represents a server mod (`package.json`) instead of a client plugin DLL.
    /// </summary>
    public required bool IsServerMod { get; init; }

    /// <summary>
    /// If this mod has an associated server/client component, stores its path.
    /// </summary>
    public string? PairedComponentPath { get; init; }

    /// <summary>
    /// Other known GUIDs associated with this mod (used for cross-referencing).
    /// </summary>
    public IReadOnlyList<string> AlternateGuids { get; init; } = [];

    /// <summary>
    /// The mod name read from the local file metadata.
    /// </summary>
    public required string LocalName { get; init; }

    /// <summary>
    /// The mod author read from the local file metadata.
    /// </summary>
    public required string LocalAuthor { get; init; }

    /// <summary>
    /// The installed version read from the local file metadata.
    /// </summary>
    public required string LocalVersion { get; init; }

    /// <summary>
    /// The SPT compatibility version read from the local file metadata, if available.
    /// </summary>
    public string? LocalSptVersion { get; init; }

    /// <summary>
    /// The mod URL read from the local file metadata (e.g. package.json), if available.
    /// </summary>
    public string? Url { get; init; }
}
