namespace CheckMods.Models;

public sealed class LocalModIdentity
{
    public required string Guid { get; init; }
    public required string FilePath { get; init; }
    public required bool IsServerMod { get; init; }
    public string? PairedComponentPath { get; set; }
    public List<string> AlternateGuids { get; init; } = [];

    public required string LocalName { get; init; }
    public required string LocalAuthor { get; init; }
    public required string LocalVersion { get; init; }
    public string? LocalSptVersion { get; init; }
}
