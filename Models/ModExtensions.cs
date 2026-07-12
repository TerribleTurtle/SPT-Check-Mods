using System.Linq;

namespace CheckModsExtended.Models;

/// <summary>
/// Extension methods for pure immutable modifications to <see cref="Mod"/> records.
/// </summary>
public static class ModExtensions
{
    /// <summary>
    /// Updates the mod with API matched details.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="apiResult">The API search result containing mod details.</param>
    /// <returns>A new <see cref="Mod"/> instance with updated API information and Verified status.</returns>
    public static Mod WithApiMatch(this Mod mod, ModSearchResult apiResult)
    {
        return mod with
        {
            Api = mod.Api with
            {
                ApiModId = apiResult.Id,
                ApiName = apiResult.Name,
                ApiAuthor = apiResult.Owner,
                ApiSlug = apiResult.Slug,
                ApiUrl = apiResult.DetailUrl,
                ApiSourceCodeUrl = apiResult.SourceCodeUrl,
                ApiVersions = apiResult.Versions?.ToList().AsReadOnly(),
            },
            Status = ModStatus.Verified,
        };
    }

    /// <summary>
    /// Marks the mod as having no match found on the API.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <returns>A new <see cref="Mod"/> instance with NoMatch status.</returns>
    public static Mod MarkUnmatched(this Mod mod)
    {
        return mod with { Status = ModStatus.NoMatch };
    }

    /// <summary>
    /// Updates the mod indicating that a safe update is available.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="update">The update information.</param>
    /// <returns>A new <see cref="Mod"/> instance with the available update details.</returns>
    public static Mod WithSafeToUpdate(this Mod mod, SafeToUpdateMod update)
    {
        var link = update.RecommendedVersion?.Link;
        if (string.IsNullOrWhiteSpace(link) && !string.IsNullOrWhiteSpace(mod.Api.ApiSourceCodeUrl))
        {
            if (System.Uri.TryCreate(mod.Api.ApiSourceCodeUrl, System.UriKind.Absolute, out var uri) &&
                uri.Host.Equals("github.com", System.StringComparison.OrdinalIgnoreCase))
            {
                var segments = uri.Segments;
                // segments[0] is "/", segments[1] is "owner/", segments[2] is "repo/"
                if (segments.Length >= 3)
                {
                    var owner = segments[1].Trim('/');
                    var repo = segments[2].Trim('/');
                    link = $"https://github.com/{owner}/{repo}/releases/latest";
                }
                else
                {
                    link = mod.Api.ApiSourceCodeUrl;
                }
            }
            else
            {
                link = mod.Api.ApiSourceCodeUrl;
            }
        }

        return mod with
        {
            Update = mod.Update with
            {
                LatestVersion = update.RecommendedVersion?.Version,
                DownloadLink = link,
                UpdateStatus = UpdateStatus.UpdateAvailable,
            },
        };
    }

    /// <summary>
    /// Updates the mod indicating that an update is blocked by other mods.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="blocked">The blocked update information.</param>
    /// <returns>A new <see cref="Mod"/> instance with the blocked update details.</returns>
    public static Mod WithBlocked(this Mod mod, BlockedUpdateMod blocked)
    {
        return mod with
        {
            Update = mod.Update with
            {
                LatestVersion = blocked.LatestVersion?.Version,
                BlockingMods = blocked.BlockingMods,
                BlockReason = blocked.BlockReason,
                UpdateStatus = UpdateStatus.UpdateBlocked,
            },
        };
    }

    /// <summary>
    /// Updates the mod indicating it is currently up-to-date.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="upToDate">The up-to-date information.</param>
    /// <returns>A new <see cref="Mod"/> instance marked as up-to-date.</returns>
    public static Mod WithUpToDate(this Mod mod, UpToDateMod upToDate)
    {
        return mod with
        {
            Update = mod.Update with { LatestVersion = upToDate.Version, UpdateStatus = UpdateStatus.UpToDate },
        };
    }

    /// <summary>
    /// Updates the mod indicating it is incompatible with the current environment.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="incompatible">The incompatibility information.</param>
    /// <returns>A new <see cref="Mod"/> instance with the incompatibility reason.</returns>
    public static Mod WithIncompatible(this Mod mod, IncompatibleMod incompatible)
    {
        return mod with
        {
            Update = mod.Update with
            {
                IncompatibilityReason = incompatible.Reason,
                UpdateStatus = UpdateStatus.Incompatible,
            },
        };
    }

    /// <summary>
    /// Updates the mod indicating it is incompatible with the local SPT version.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="reason">The reason for incompatibility.</param>
    /// <param name="compatibleVersion">The specific version that would be compatible, if known.</param>
    /// <returns>A new <see cref="Mod"/> instance marked as locally SPT incompatible.</returns>
    public static Mod WithLocalSptIncompatible(this Mod mod, string reason, string? compatibleVersion = null)
    {
        return mod with
        {
            Update = mod.Update with
            {
                IsLocalSptIncompatible = true,
                IncompatibilityReason = reason,
                CompatibleVersionString = compatibleVersion,
            },
        };
    }

    /// <summary>
    /// Toggles the update suppression state of the mod.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="suppressed">Whether updates should be suppressed.</param>
    /// <returns>A new <see cref="Mod"/> instance with the updated suppression state.</returns>
    public static Mod WithUpdateSuppressed(this Mod mod, bool suppressed)
    {
        return mod with { Update = mod.Update with { UpdateSuppressed = suppressed } };
    }

    /// <summary>
    /// Updates the mod with dependency delta changes required for an update.
    /// </summary>
    /// <param name="mod">The mod to modify.</param>
    /// <param name="delta">The dependency changes delta.</param>
    /// <returns>A new <see cref="Mod"/> instance containing the dependency delta.</returns>
    public static Mod WithUpdateDependencyChanges(this Mod mod, UpdateDependencyDelta delta)
    {
        return mod with { Update = mod.Update with { UpdateDependencyChanges = delta } };
    }
}
