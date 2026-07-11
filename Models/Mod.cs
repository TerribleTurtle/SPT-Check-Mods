namespace CheckMods.Models;

/// <summary>
/// Master record representing a mod throughout the entire processing lifecycle.
/// </summary>
public sealed class Mod
{
    public required LocalModIdentity Local { get; init; }
    public ForgeApiMetadata Api { get; } = new();
    public ModUpdateState Update { get; } = new();

    #region Processing State

    public ModStatus Status { get; private set; } = ModStatus.NoMatch;

    public List<string> LoadWarnings { get; init; } = [];

    #endregion

    #region Display Properties (computed)

    /// <summary>
    /// The preferred display name (API name if available, otherwise local name).
    /// </summary>
    public string DisplayName
    {
        get { return Api.ApiName ?? Local.LocalName; }
    }

    /// <summary>
    /// The preferred display author (API author if available, otherwise local author).
    /// </summary>
    public string DisplayAuthor
    {
        get { return Api.ApiAuthor?.Name ?? Local.LocalAuthor; }
    }

    public bool HasWarnings
    {
        get { return LoadWarnings.Count > 0; }
    }

    public bool IsMatched
    {
        get { return Status == ModStatus.Verified && Api.ApiModId.HasValue; }
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Updates the mod with API match data from a search result.
    /// </summary>
    /// <param name="apiResult">The API search result to populate from.</param>
    public void UpdateFromApiMatch(ModSearchResult apiResult)
    {
        Api.ApiModId = apiResult.Id;
        Api.ApiName = apiResult.Name;
        Api.ApiAuthor = apiResult.Owner;
        Api.ApiSlug = apiResult.Slug;
        Api.ApiUrl = apiResult.DetailUrl;
        Api.ApiSourceCodeUrl = apiResult.SourceCodeUrl;
        Api.ApiVersions = apiResult.Versions?.ToList().AsReadOnly();

        Status = ModStatus.Verified;
    }

    /// <summary>
    /// Marks the mod as having no Forge API match.
    /// </summary>
    public void MarkUnmatched()
    {
        Status = ModStatus.NoMatch;
    }

    /// <summary>
    /// Updates the mod with safe-to-update information from the batch updates endpoint.
    /// </summary>
    /// <param name="update">The update information from the API.</param>
    public void UpdateFromSafeToUpdate(SafeToUpdateMod update)
    {
        Update.LatestVersion = update.RecommendedVersion?.Version;
        Update.DownloadLink = update.RecommendedVersion?.Link;
        Update.UpdateStatus = UpdateStatus.UpdateAvailable;
    }

    /// <summary>
    /// Updates the mod with blocked update information from the batch updates endpoint.
    /// </summary>
    /// <param name="blocked">The blocked update information from the API.</param>
    public void UpdateFromBlocked(BlockedUpdateMod blocked)
    {
        Update.LatestVersion = blocked.LatestVersion?.Version;
        Update.BlockingMods = blocked.BlockingMods;
        Update.BlockReason = blocked.BlockReason;
        Update.UpdateStatus = UpdateStatus.UpdateBlocked;
    }

    /// <summary>
    /// Updates the mod with up-to-date information from the batch updates endpoint.
    /// </summary>
    /// <param name="upToDate">The up-to-date information from the API.</param>
    public void UpdateFromUpToDate(UpToDateMod upToDate)
    {
        Update.LatestVersion = upToDate.Version;
        Update.UpdateStatus = UpdateStatus.UpToDate;
    }

    /// <summary>
    /// Updates the mod with incompatibility information from the batch updates endpoint.
    /// </summary>
    /// <param name="incompatible">The incompatibility information from the API.</param>
    public void UpdateFromIncompatible(IncompatibleMod incompatible)
    {
        Update.IncompatibilityReason = incompatible.Reason;
        Update.UpdateStatus = UpdateStatus.Incompatible;
    }

    /// <summary>
    /// Marks the mod as locally incompatible with the installed SPT version.
    /// </summary>
    /// <param name="reason">The reason for incompatibility.</param>
    /// <param name="compatibleVersion">The version string of a compatible version, if available.</param>
    public void SetLocalSptIncompatible(string reason, string? compatibleVersion = null)
    {
        Update.IsLocalSptIncompatible = true;
        Update.IncompatibilityReason = reason;
        Update.CompatibleVersionString = compatibleVersion;
    }

    /// <summary>
    /// Sets whether this mod's available update is suppressed (dismissed as a false positive).
    /// </summary>
    /// <param name="suppressed">True to treat the available update as ignored; false to show it normally.</param>
    public void SetUpdateSuppressed(bool suppressed)
    {
        Update.UpdateSuppressed = suppressed;
    }

    /// <summary>
    /// Records how the proposed update changes this mod's dependencies compared to the installed version.
    /// </summary>
    /// <param name="delta">The added/removed dependency changes introduced by the update.</param>
    public void SetUpdateDependencyChanges(UpdateDependencyDelta delta)
    {
        Update.UpdateDependencyChanges = delta;
    }

    #endregion
}
