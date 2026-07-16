using System.Text.Json.Serialization;

namespace CheckModsExtended.Models;

/// <summary>
/// Where an <see cref="IgnoredUpdate"/> entry came from.
/// </summary>
public enum IgnoreSource
{
    /// <summary>The user dismissed this update locally.</summary>
    User,

    /// <summary>The entry was merged in from the author-maintained remote base list.</summary>
    Remote,
}

/// <summary>
/// A single dismissed ("ignored") mod update. Identifies a known false-positive update where the Forge version is
/// higher than the installed DLL version but the distributed files are actually the same. Matching uses the triple
/// (<see cref="ApiModId"/>, <see cref="LocalVersion"/>, <see cref="IgnoredLatestVersion"/>); the remaining fields are
/// metadata.
/// </summary>
/// <param name="ApiModId">The Forge API mod ID.</param>
/// <param name="LocalVersion">The locally installed version string.</param>
/// <param name="IgnoredLatestVersion">The remote latest version string to ignore.</param>
/// <param name="Name">The display name of the mod.</param>
/// <param name="Guid">The unique identifier of the mod.</param>
/// <param name="Source">Where the ignored update came from (User or Remote).</param>
/// <param name="DismissedUtc">When the update was dismissed.</param>
public sealed record IgnoredUpdate(
    [property: JsonPropertyName("apiModId")] int ApiModId,
    [property: JsonPropertyName("localVersion")] string LocalVersion,
    [property: JsonPropertyName("ignoredLatestVersion")] string IgnoredLatestVersion,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("guid")] string? Guid = null,
    [property: JsonPropertyName("source")] IgnoreSource Source = IgnoreSource.User,
    [property: JsonPropertyName("dismissedUtc")] DateTimeOffset? DismissedUtc = null
)
{
    /// <summary>
    /// The match key: API mod id plus both version strings. Compared with <see cref="StringComparer.OrdinalIgnoreCase"/>
    /// by callers.
    /// </summary>
    [JsonIgnore]
    public string Key
    {
        get { return $"{ApiModId}|{LocalVersion}|{IgnoredLatestVersion}"; }
    }

    /// <summary>
    /// Whether this entry carries the minimum data needed to match a mod.
    /// </summary>
    [JsonIgnore]
    public bool IsWellFormed
    {
        get
        {
            return ApiModId > 0
                && !string.IsNullOrWhiteSpace(LocalVersion)
                && !string.IsNullOrWhiteSpace(IgnoredLatestVersion);
        }
    }
}

/// <summary>
/// The on-disk (and remote) document format for the ignored-updates list.
/// </summary>
/// <param name="SchemaVersion">The version of the JSON schema.</param>
/// <param name="Ignored">The list of dismissed update records.</param>
public sealed record IgnoredUpdatesFile(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("ignored")] List<IgnoredUpdate> Ignored
)
{
    /// <summary>The schema version this build reads and writes.</summary>
    public const int CurrentSchemaVersion = 1;
}
