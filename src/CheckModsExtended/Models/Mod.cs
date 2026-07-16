namespace CheckModsExtended.Models;

/// <summary>
/// The central entity representing a mod throughout the entire processing lifecycle.
/// </summary>
public sealed record Mod
{
    /// <summary>
    /// Metadata describing the local installation state of the mod on the disk.
    /// </summary>
    public required LocalModIdentity Local { get; init; }

    /// <summary>
    /// Information retrieved from the remote Forge API for this mod, if matched.
    /// </summary>
    public ForgeApiMetadata Api { get; init; } = new();

    /// <summary>
    /// Tracks the update status of this mod against available versions.
    /// </summary>
    public ModUpdateState Update { get; init; } = new();

    #region Processing State

    /// <summary>
    /// The current processing state of the mod.
    /// </summary>
    public ModStatus Status { get; init; } = ModStatus.NoMatch;

    /// <summary>
    /// A list of warnings generated during the mod loading phase.
    /// </summary>
    public IReadOnlyList<string> LoadWarnings { get; init; } = [];

    /// <summary>
    /// Indicates whether this mod is a duplicate installation of another mod.
    /// </summary>
    public bool IsDuplicate { get; init; }

    #endregion

    #region Display Properties (computed)

    /// <summary>
    /// The preferred display name (API name if available, otherwise local name).
    /// </summary>
    public string DisplayName
    {
        get { return Api.ApiName ?? Local.LocalName; }
    }

    /// <summary>
    /// The preferred display author (API author if available, otherwise local author).
    /// </summary>
    public string DisplayAuthor
    {
        get { return Api.ApiAuthor?.Name ?? Local.LocalAuthor; }
    }

    /// <summary>
    /// Gets a value indicating whether this mod has any load warnings.
    /// </summary>
    public bool HasWarnings
    {
        get { return LoadWarnings.Count > 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this mod has been matched with an API entry.
    /// </summary>
    public bool IsMatched
    {
        get { return Status == ModStatus.Verified && Api.ApiModId.HasValue; }
    }

    #endregion
}
