namespace CheckMods.Models;

/// <summary>
/// Contains metadata retrieved from the remote Forge API.
/// </summary>
public sealed class ForgeApiMetadata
{
    /// <summary>
    /// The numeric ID assigned to the mod on the Forge API.
    /// </summary>
    public int? ApiModId { get; set; }

    /// <summary>
    /// The display name of the mod as registered on the Forge API.
    /// </summary>
    public string? ApiName { get; set; }

    /// <summary>
    /// The author of the mod as registered on the Forge API.
    /// </summary>
    public ModAuthor? ApiAuthor { get; set; }

    /// <summary>
    /// The URL slug for the mod on the Forge API.
    /// </summary>
    public string? ApiSlug { get; set; }

    /// <summary>
    /// The URL to the mod's detail page.
    /// </summary>
    public string? ApiUrl { get; set; }

    /// <summary>
    /// The URL to the mod's source code, if provided.
    /// </summary>
    public string? ApiSourceCodeUrl { get; set; }

    /// <summary>
    /// The list of released versions available on the Forge API.
    /// </summary>
    public IReadOnlyList<ModVersion>? ApiVersions { get; set; }
}
