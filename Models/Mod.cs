namespace CheckMods.Models;

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

    public ModStatus Status { get; init; } = ModStatus.NoMatch;

    public IReadOnlyList<string> LoadWarnings { get; init; } = [];

    #endregion

    #region Display Properties (computed)

    /// <summary>
    /// The preferred display name (API name if available, otherwise local name).
    /// </summary>
    public string DisplayName => Api.ApiName ?? Local.LocalName;

    /// <summary>
    /// The preferred display author (API author if available, otherwise local author).
    /// </summary>
    public string DisplayAuthor => Api.ApiAuthor?.Name ?? Local.LocalAuthor;

    public bool HasWarnings => LoadWarnings.Count > 0;

    public bool IsMatched => Status == ModStatus.Verified && Api.ApiModId.HasValue;

    #endregion
}
