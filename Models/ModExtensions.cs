using System.Linq;

namespace CheckModsExtended.Models;

/// <summary>
/// Extension methods for pure immutable modifications to <see cref="Mod"/> records.
/// </summary>
public static class ModExtensions
{
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
                ApiVersions = apiResult.Versions?.ToList().AsReadOnly()
            },
            Status = ModStatus.Verified
        };
    }

    public static Mod MarkUnmatched(this Mod mod)
    {
        return mod with { Status = ModStatus.NoMatch };
    }

    public static Mod WithSafeToUpdate(this Mod mod, SafeToUpdateMod update)
    {
        return mod with
        {
            Update = mod.Update with
            {
                LatestVersion = update.RecommendedVersion?.Version,
                DownloadLink = update.RecommendedVersion?.Link,
                UpdateStatus = UpdateStatus.UpdateAvailable
            }
        };
    }

    public static Mod WithBlocked(this Mod mod, BlockedUpdateMod blocked)
    {
        return mod with
        {
            Update = mod.Update with
            {
                LatestVersion = blocked.LatestVersion?.Version,
                BlockingMods = blocked.BlockingMods,
                BlockReason = blocked.BlockReason,
                UpdateStatus = UpdateStatus.UpdateBlocked
            }
        };
    }

    public static Mod WithUpToDate(this Mod mod, UpToDateMod upToDate)
    {
        return mod with
        {
            Update = mod.Update with
            {
                LatestVersion = upToDate.Version,
                UpdateStatus = UpdateStatus.UpToDate
            }
        };
    }

    public static Mod WithIncompatible(this Mod mod, IncompatibleMod incompatible)
    {
        return mod with
        {
            Update = mod.Update with
            {
                IncompatibilityReason = incompatible.Reason,
                UpdateStatus = UpdateStatus.Incompatible
            }
        };
    }

    public static Mod WithLocalSptIncompatible(this Mod mod, string reason, string? compatibleVersion = null)
    {
        return mod with
        {
            Update = mod.Update with
            {
                IsLocalSptIncompatible = true,
                IncompatibilityReason = reason,
                CompatibleVersionString = compatibleVersion
            }
        };
    }

    public static Mod WithUpdateSuppressed(this Mod mod, bool suppressed)
    {
        return mod with
        {
            Update = mod.Update with
            {
                UpdateSuppressed = suppressed
            }
        };
    }

    public static Mod WithUpdateDependencyChanges(this Mod mod, UpdateDependencyDelta delta)
    {
        return mod with
        {
            Update = mod.Update with
            {
                UpdateDependencyChanges = delta
            }
        };
    }
}

