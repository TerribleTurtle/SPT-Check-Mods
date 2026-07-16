using System.Text.Json.Serialization;

namespace CheckModsExtended.Models;

/// <summary>
/// Response from the Forge API mod dependencies endpoint.
/// </summary>
/// <param name="Success">Whether the API request was successful.</param>
/// <param name="Data">The list of mod dependencies.</param>
public sealed record ModDependenciesApiResponse(
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
public sealed record ModDependency(
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
public sealed record DependencyVersionInfo(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("link")] string? Link,
    [property: JsonPropertyName("content_length")] long? ContentLength,
    [property: JsonPropertyName("fika_compatibility")] string? FikaCompatibility
);
