using System.Text.Json.Serialization;

namespace CheckMods.Models;

/// <summary>
/// Response from the Forge API mod search endpoint.
/// </summary>
/// <param name="Success">Whether the API request was successful.</param>
/// <param name="Data">The list of mod search results.</param>
public record ModSearchApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<ModSearchResult>? Data
);

/// <summary>
/// Represents a mod search result from the Forge API.
/// </summary>
/// <param name="Id">The numeric ID of the mod.</param>
/// <param name="HubId">The hub ID of the mod, if applicable.</param>
/// <param name="Name">The display name of the mod.</param>
/// <param name="Slug">The URL slug for the mod.</param>
/// <param name="Teaser">A short description or teaser of the mod.</param>
/// <param name="Thumbnail">The URL to the mod's thumbnail image.</param>
/// <param name="Downloads">The total number of downloads.</param>
/// <param name="SourceCodeLinks">Links to the mod's source code repositories.</param>
/// <param name="DetailUrl">The URL to the mod's detail page.</param>
/// <param name="Owner">The author or owner of the mod.</param>
/// <param name="Versions">A list of available versions for the mod.</param>
public record ModSearchResult(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("hub_id")] int? HubId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("teaser")] string? Teaser,
    [property: JsonPropertyName("thumbnail")] string? Thumbnail,
    [property: JsonPropertyName("downloads")] int Downloads,
    [property: JsonPropertyName("source_code_links")] List<SourceCodeLink>? SourceCodeLinks,
    [property: JsonPropertyName("detail_url")] string? DetailUrl,
    [property: JsonPropertyName("owner")] ModAuthor? Owner,
    [property: JsonPropertyName("versions")] List<ModVersion>? Versions
)
{
    /// <summary>
    /// Gets the primary source code URL (first link if available).
    /// </summary>
    public string? SourceCodeUrl
    {
        get { return SourceCodeLinks?.FirstOrDefault()?.Url; }
    }
}

/// <summary>
/// Represents the author/owner of a mod.
/// </summary>
/// <param name="Id">The numeric ID of the author.</param>
/// <param name="Name">The display name of the author.</param>
/// <param name="ProfilePhotoUrl">The URL to the author's profile photo.</param>
public record ModAuthor(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("profile_photo_url")] string? ProfilePhotoUrl
);

/// <summary>
/// Represents a link to a source code repository.
/// </summary>
/// <param name="Url">The URL to the repository.</param>
/// <param name="Label">An optional label for the link.</param>
public record SourceCodeLink(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("label")] string? Label
);

/// <summary>
/// Represents a specific version of a mod.
/// </summary>
/// <param name="Id">The numeric ID of the version.</param>
/// <param name="HubId">The hub ID associated with the version, if applicable.</param>
/// <param name="Version">The version string.</param>
/// <param name="Description">A description or changelog for the version.</param>
/// <param name="Link">A download link for the version.</param>
/// <param name="SptVersionConstraint">The SPT version constraint expression.</param>
/// <param name="VirusTotalLink">A link to the VirusTotal scan results.</param>
/// <param name="Downloads">The number of downloads for this version.</param>
/// <param name="PublishedAt">The ISO 8601 timestamp when the version was published.</param>
/// <param name="CreatedAt">The ISO 8601 timestamp when the version was created.</param>
/// <param name="UpdatedAt">The ISO 8601 timestamp when the version was last updated.</param>
public record ModVersion(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("hub_id")] int? HubId,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("spt_version_constraint")] string SptVersionConstraint,
    [property: JsonPropertyName("virus_total_link")] string? VirusTotalLink,
    [property: JsonPropertyName("downloads")] int Downloads,
    [property: JsonPropertyName("published_at")] string? PublishedAt,
    [property: JsonPropertyName("created_at")] string? CreatedAt,
    [property: JsonPropertyName("updated_at")] string? UpdatedAt
);

/// <summary>
/// Response from the Forge API mod versions' endpoint.
/// </summary>
/// <param name="Success">Whether the API request was successful.</param>
/// <param name="Data">The list of mod versions.</param>
public record ModVersionsApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<ModVersion>? Data
);

#region Batch Updates Endpoint Models

/// <summary>
/// Response from the Forge API batch mod updates endpoint.
/// </summary>
/// <param name="Success">Whether the API request was successful.</param>
/// <param name="Data">The categorized batch updates data.</param>
public record ModUpdatesApiResponse(
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
public record ModUpdatesData(
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
public record ModUpdateVersion(
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
public record SafeToUpdateMod(
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
public record BlockedUpdateMod(
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
public record BlockingModInfo(
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
public record UpToDateMod(
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
public record IncompatibleMod(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("mod_id")] int ModId,
    [property: JsonPropertyName("guid")] string? Guid,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("reason")] string Reason,
    [property: JsonPropertyName("latest_compatible_version")] ModUpdateVersion? LatestCompatibleVersion
);

#endregion

#region Dependencies Endpoint Models

/// <summary>
/// Response from the Forge API mod dependencies endpoint.
/// </summary>
/// <param name="Success">Whether the API request was successful.</param>
/// <param name="Data">The list of mod dependencies.</param>
public record ModDependenciesApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<ModDependency>? Data
);

/// <summary>
/// Represents a dependency from the Forge API dependencies endpoint.
/// </summary>
/// <param name="Id">The numeric ID of the dependency.</param>
/// <param name="Guid">The unique identifier of the dependency.</param>
/// <param name="Name">The display name of the dependency.</param>
/// <param name="Slug">The URL slug for the dependency.</param>
/// <param name="LatestCompatibleVersion">The latest compatible version of the dependency.</param>
/// <param name="Conflict">Whether there is a conflict with this dependency.</param>
/// <param name="Dependencies">A nested list of further dependencies.</param>
public record ModDependency(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("guid")] string Guid,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("latest_compatible_version")] DependencyVersionInfo? LatestCompatibleVersion,
    [property: JsonPropertyName("conflict")] bool Conflict,
    [property: JsonPropertyName("dependencies")] List<ModDependency>? Dependencies
);

/// <summary>
/// Version information for a dependency.
/// </summary>
/// <param name="Id">The numeric ID of the version.</param>
/// <param name="Version">The version string.</param>
/// <param name="Link">A download link for the version.</param>
/// <param name="ContentLength">The file size of the download in bytes.</param>
/// <param name="FikaCompatibility">Compatibility information with the Fika mod.</param>
public record DependencyVersionInfo(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("content_length")] long? ContentLength,
    [property: JsonPropertyName("fika_compatibility")] string? FikaCompatibility
);

#endregion
