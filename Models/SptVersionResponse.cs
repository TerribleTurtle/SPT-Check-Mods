using System.Text.Json.Serialization;

namespace CheckMods.Models;

/// <summary>
/// Response from the Forge API SPT versions endpoint.
/// </summary>
/// <param name="Success">Whether the request was successful.</param>
/// <param name="Data">Available SPT versions returned by the API.</param>
public sealed record SptVersionApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<SptVersionResult>? Data
);

/// <summary>
/// Represents an SPT version from the Forge API.
/// </summary>
/// <param name="Id">The unique numeric identifier for this SPT version on the Forge API.</param>
/// <param name="Version">The full version string (e.g., "3.9.0").</param>
/// <param name="VersionMajor">The parsed major version number.</param>
/// <param name="VersionMinor">The parsed minor version number.</param>
/// <param name="VersionPatch">The parsed patch version number.</param>
/// <param name="VersionLabels">Additional version labels (e.g., "beta", "rc").</param>
/// <param name="ModCount">The number of mods available for this SPT version.</param>
/// <param name="Link">Download or detail link for this SPT version.</param>
/// <param name="ColorClass">CSS color class.</param>
/// <param name="CreatedAt">ISO 8601 timestamp when this version was created.</param>
/// <param name="UpdatedAt">ISO 8601 timestamp when this version was last updated.</param>
public sealed record SptVersionResult(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("version_major")] int VersionMajor,
    [property: JsonPropertyName("version_minor")] int VersionMinor,
    [property: JsonPropertyName("version_patch")] int VersionPatch,
    [property: JsonPropertyName("version_labels")] string VersionLabels,
    [property: JsonPropertyName("mod_count")] int ModCount,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("color_class")] string? ColorClass,
    [property: JsonPropertyName("created_at")] string? CreatedAt,
    [property: JsonPropertyName("updated_at")] string? UpdatedAt
);
