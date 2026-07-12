namespace CheckModsExtended.Configuration;

/// <summary>
/// Configuration for the ignored-updates feature: where the local list is stored and where the optional
/// author-maintained remote base list is fetched from.
/// </summary>
public sealed class IgnoredUpdateOptions
{
    /// <summary>The path to the local ignored-updates file. Relative paths resolve against AppDataDirectory.</summary>
    public string FilePath { get; set; } = "ignored-updates.json";

    /// <summary>
    /// URL of the author-maintained remote base list, or null/empty to disable the remote-fetch prompt entirely.
    /// </summary>
    public string? RemoteUrl { get; set; } = "https://forge-static.sp-tarkov.com/check-mods/ignored-updates.json";

    /// <summary>Timeout for the remote fetch, in seconds.</summary>
    public int RemoteTimeoutSeconds { get; set; } = 10;
}
