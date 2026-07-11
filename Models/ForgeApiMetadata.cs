namespace CheckModsExtended.Models;

/// <summary>
/// Contains metadata retrieved from the remote Forge API.
/// </summary>
public sealed record ForgeApiMetadata
{
    /// <summary>
    /// The numeric ID assigned to the mod on the Forge API.
    /// </summary>
    public int? ApiModId { get; init; }

    /// <summary>
    /// The display name of the mod as registered on the Forge API.
    /// </summary>
    public string? ApiName { get; init; }

    /// <summary>
    /// The author of the mod as registered on the Forge API.
    /// </summary>
    public ModAuthor? ApiAuthor { get; init; }

    /// <summary>
    /// The URL slug for the mod on the Forge API.
    /// </summary>
    public string? ApiSlug { get; init; }

    /// <summary>
    /// The URL to the mod's detail page.
    /// </summary>
    public string? ApiUrl { get; init; }

    /// <summary>
    /// The URL to the mod's source code, if provided.
    /// </summary>
    public string? ApiSourceCodeUrl { get; init; }

    /// <summary>
    /// The list of released versions available on the Forge API.
    /// </summary>
    public IReadOnlyList<ModVersion>? ApiVersions { get; init; }
}

