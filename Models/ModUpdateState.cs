namespace CheckModsExtended.Models;

/// <summary>
/// Tracks the update availability, blocking status, and compatibility of a mod.
/// </summary>
public sealed record ModUpdateState
{
    /// <summary>
    /// The latest version of the mod available for the installed SPT version.
    /// </summary>
    public string? LatestVersion { get; init; }

    /// <summary>
    /// The current update status for the mod (e.g., UpToDate, UpdateAvailable, UpdateBlocked).
    /// </summary>
    public UpdateStatus UpdateStatus { get; init; } = UpdateStatus.Unknown;

    /// <summary>
    /// A direct or indirect link to download the update.
    /// </summary>
    public string? DownloadLink { get; init; }

    /// <summary>
    /// If an update is blocked, the list of mods causing the blockage.
    /// </summary>
    public IReadOnlyList<BlockingModInfo>? BlockingMods { get; init; }

    /// <summary>
    /// The reason why the update is blocked, if applicable.
    /// </summary>
    public string? BlockReason { get; init; }

    /// <summary>
    /// The reason why the mod is incompatible with the installed SPT version.
    /// </summary>
    public string? IncompatibilityReason { get; init; }

    /// <summary>
    /// True if the currently installed version is locally incompatible with the installed SPT version.
    /// </summary>
    public bool IsLocalSptIncompatible { get; init; }

    /// <summary>
    /// A compatible version string for the SPT installation, if available when incompatible.
    /// </summary>
    public string? CompatibleVersionString { get; init; }

    /// <summary>
    /// True if the user has chosen to ignore/suppress this specific update notification.
    /// </summary>
    public bool UpdateSuppressed { get; init; }

    /// <summary>
    /// Contains information about how an update alters the mod's dependencies.
    /// </summary>
    public UpdateDependencyDelta? UpdateDependencyChanges { get; init; }
}
