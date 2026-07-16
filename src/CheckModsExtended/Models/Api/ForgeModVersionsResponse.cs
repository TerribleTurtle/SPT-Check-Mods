using System.Text.Json.Serialization;

namespace CheckModsExtended.Models;

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
public sealed record ModVersion(
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
public sealed record ModVersionsApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] List<ModVersion>? Data
);
