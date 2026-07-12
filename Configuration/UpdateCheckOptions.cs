namespace CheckModsExtended.Configuration;

/// <summary>
/// Configuration options for the Check Mods self-update check.
/// </summary>
public sealed class UpdateCheckOptions
{
    /// <summary>
    /// The Forge mod ID for Check Mods itself (points to the Check Mods Extended project for self-updates).
    /// </summary>
    public int ForgeModId { get; set; } = 2471;
}
