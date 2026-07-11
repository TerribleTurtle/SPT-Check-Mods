namespace CheckMods.Models;

public sealed class ForgeApiMetadata
{
    public int? ApiModId { get; set; }
    public string? ApiName { get; set; }
    public ModAuthor? ApiAuthor { get; set; }
    public string? ApiSlug { get; set; }
    public string? ApiUrl { get; set; }
    public string? ApiSourceCodeUrl { get; set; }
    public IReadOnlyList<ModVersion>? ApiVersions { get; set; }
}
