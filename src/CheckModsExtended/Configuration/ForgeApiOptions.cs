namespace CheckModsExtended.Configuration;

/// <summary>
/// Configuration options for the Forge API service.
/// </summary>
public sealed class ForgeApiOptions
{
    /// <summary>
    /// The base URL for the Forge API (e.g., https://forge.sp-tarkov.com/api/v0/).
    /// </summary>
    public string BaseUrl { get; set; } = "https://forge.sp-tarkov.com/api/v0/";
}
