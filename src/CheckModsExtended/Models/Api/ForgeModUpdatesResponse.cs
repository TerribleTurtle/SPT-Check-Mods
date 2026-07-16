using System.Text.Json.Serialization;

namespace CheckModsExtended.Models;

/// <summary>
/// Response from the Forge API batch mod updates endpoint.
/// </summary>
/// <param name="Success">Whether the API request was successful.</param>
/// <param name="Data">The categorized batch updates data.</param>
public sealed record ModUpdatesApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] ModUpdatesData? Data
);

/// <summary>
/// Categorized mod update information from the batch updates endpoint.
/// </summary>
/// <param name="SafeToUpdate">A list of mods that are safe to update.</param>
/// <param name="Blocked">A list of mods whose updates are blocked by dependencies.</param>
/// <param name="UpToDate">A list of mods that are already up to date.</param>
/// <param name="Incompatible">A list of mods that are incompatible with the installed SPT version.</param>
public sealed record ModUpdatesData(
    [property: JsonPropertyName("updates")] List<SafeToUpdateMod>? SafeToUpdate,
    [property: JsonPropertyName("blocked_updates")] List<BlockedUpdateMod>? Blocked,
    [property: JsonPropertyName("up_to_date")] List<UpToDateMod>? UpToDate,
    [property: JsonPropertyName("incompatible_with_spt")] List<IncompatibleMod>? Incompatible
);

/// <summary>
/// A version reference (current, recommended, latest, or latest-compatible) returned within the batch updates and
/// incompatibility responses.
/// </summary>
/// <param name="Id">The numeric ID of the version.</param>
/// <param name="ModId">The Forge mod ID associated with the version.</param>
/// <param name="Guid">The unique identifier of the mod.</param>
/// <param name="Name">The display name of the mod.</param>
/// <param name="Slug">The URL slug of the mod.</param>
/// <param name="Version">The version string.</param>
/// <param name="Link">The download link for this version.</param>
/// <param name="SptVersions">A list of compatible SPT versions.</param>
public sealed record ModUpdateVersion(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("mod_id")] int ModId,
    [property: JsonPropertyName("guid")] string? Guid,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("slug")] string? Slug,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("spt_versions")] List<string>? SptVersions
);

/// <summary>
/// A mod that has an update available and is safe to update.
/// </summary>
/// <param name="CurrentVersion">Information about the currently installed version.</param>
/// <param name="RecommendedVersion">Information about the recommended update version.</param>
/// <param name="UpdateReason">The reason why the update is recommended.</param>
public sealed record SafeToUpdateMod(
    [property: JsonPropertyName("current_version")] ModUpdateVersion? CurrentVersion,
    [property: JsonPropertyName("recommended_version")] ModUpdateVersion? RecommendedVersion,
    [property: JsonPropertyName("update_reason")] string? UpdateReason
)
{
    /// <summary>
    /// The mod ID, from the current version.
    /// </summary>
    public int ModId
    {
        get { return CurrentVersion?.ModId ?? 0; }
    }
}

/// <summary>
/// A mod that has an update available but is blocked by dependency constraints.
/// </summary>
/// <param name="CurrentVersion">Information about the currently installed version.</param>
/// <param name="LatestVersion">Information about the latest version that is blocked.</param>
/// <param name="BlockReason">A description of why the update is blocked.</param>
/// <param name="BlockingMods">A list of the specific mods causing the blockage.</param>
public sealed record BlockedUpdateMod(
    [property: JsonPropertyName("current_version")] ModUpdateVersion? CurrentVersion,
    [property: JsonPropertyName("latest_version")] ModUpdateVersion? LatestVersion,
    [property: JsonPropertyName("block_reason")] string? BlockReason,
    [property: JsonPropertyName("blocking_mods")] List<BlockingModInfo>? BlockingMods
)
{
    /// <summary>
    /// The mod ID, from the current version.
    /// </summary>
    public int ModId
    {
        get { return CurrentVersion?.ModId ?? 0; }
    }
}

/// <summary>
/// Information about a mod that is blocking an update due to dependency constraints.
/// </summary>
/// <param name="ModId">The Forge mod ID of the blocking mod.</param>
/// <param name="ModGuid">The GUID of the blocking mod.</param>
/// <param name="Name">The display name of the blocking mod.</param>
/// <param name="CurrentVersion">The currently installed version of the blocking mod.</param>
/// <param name="Constraint">The version constraint that is causing the blockage.</param>
/// <param name="IncompatibleWith">The component the mod is incompatible with.</param>
public sealed record BlockingModInfo(
    [property: JsonPropertyName("mod_id")] int ModId,
    [property: JsonPropertyName("mod_guid")] string? ModGuid,
    [property: JsonPropertyName("mod_name")] string Name,
    [property: JsonPropertyName("current_version")] string? CurrentVersion,
    [property: JsonPropertyName("constraint")] string Constraint,
    [property: JsonPropertyName("incompatible_with")] string? IncompatibleWith
);

/// <summary>
/// A mod that is already up to date.
/// </summary>
/// <param name="Id">The numeric ID of the version.</param>
/// <param name="ModId">The Forge mod ID associated with the version.</param>
/// <param name="Guid">The unique identifier of the mod.</param>
/// <param name="Name">The display name of the mod.</param>
/// <param name="Version">The version string.</param>
/// <param name="SptVersions">A list of compatible SPT versions.</param>
public sealed record UpToDateMod(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("mod_id")] int ModId,
    [property: JsonPropertyName("guid")] string? Guid,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("spt_versions")] List<string>? SptVersions
);

/// <summary>
/// A mod that has no compatible version for the current SPT version.
/// </summary>
/// <param name="Id">The numeric ID of the version.</param>
/// <param name="ModId">The Forge mod ID associated with the version.</param>
/// <param name="Guid">The unique identifier of the mod.</param>
/// <param name="Name">The display name of the mod.</param>
/// <param name="Version">The version string.</param>
/// <param name="Reason">The reason why the mod is incompatible.</param>
/// <param name="LatestCompatibleVersion">The latest version of the mod that was compatible, if any.</param>
public sealed record IncompatibleMod(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("mod_id")] int ModId,
    [property: JsonPropertyName("guid")] string? Guid,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("latest_compatible_version")] ModUpdateVersion? LatestCompatibleVersion
);
