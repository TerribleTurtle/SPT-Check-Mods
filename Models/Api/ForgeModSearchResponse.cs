using System.Text.Json.Serialization;

namespace CheckModsExtended.Models;

/// <summary>
/// Response from the Forge API mod search endpoint.
/// </summary>
/// <param name="Success">Whether the API request was successful.</param>
/// <param name="Data">The list of mod search results.</param>
public sealed record ModSearchApiResponse(
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
public sealed record ModSearchResult(
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
public sealed record ModAuthor(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("profile_photo_url")] string? ProfilePhotoUrl
);

/// <summary>
/// Represents a link to a source code repository.
/// </summary>
/// <param name="Url">The URL to the repository.</param>
/// <param name="Label">An optional label for the link.</param>
public sealed record SourceCodeLink(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("label")] string? Label
);

