using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SemanticVersioning;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Service responsible for enriching matched mods with additional API data such as version information.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ModEnrichmentService(
    IModUpdateClient forgeApiService,
    IGitHubReleaseClient gitHubReleaseClient,
    ILogger<ModEnrichmentService> logger,
    IModLinkResolverService linkResolver)
    : IModEnrichmentService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Mod>> EnrichAllWithVersionDataAsync(
        IEnumerable<Mod> mods,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Enriching mods with version data");

        var matchedMods = mods.Where(m => m.IsMatched && m.Api.ApiModId.HasValue).ToList();

        // Group by API mod ID to deduplicate.
        var uniqueModsById = matchedMods
            .GroupBy(m => m.Api.ApiModId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(m =>
                    CheckModsExtended.Utils.SemVer.TryParse(m.Local.LocalVersion, "ModEnrichment").Match(v => v, _ => new SemanticVersioning.Version(0, 0, 0))
                ).ToList()
            );

        if (uniqueModsById.Count == 0)
        {
            logger.LogDebug("No matched mods to enrich");
            return mods.ToList();
        }

        logger.LogDebug("Enriching {ModCount} unique mods", uniqueModsById.Count);

        var modUpdates = uniqueModsById
            .Select(kvp => (ModId: kvp.Key, CurrentVersion: kvp.Value[0].Local.LocalVersion))
            .ToList();

        var updatesResult = await forgeApiService.GetModUpdatesAsync(modUpdates, sptVersion, cancellationToken);

        if (!updatesResult.TryPickT0(out var updatesData, out _))
        {
            return mods.ToList();
        }

        var modsDict = mods.ToDictionary(m => m.Local.Guid);

        /// <summary>
        /// Processes updates for a generic collection of items in batches to avoid rate limits.
        /// </summary>
        void ProcessUpdates<T>(IEnumerable<T>? updates, Func<T, int> getModId, Func<Mod, T, Mod> updateAction)
        {
            if (updates is null)
            {
                return;
            }

            var modsToUpdate = updates
                .Where(u => uniqueModsById.ContainsKey(getModId(u)))
                .SelectMany(u => uniqueModsById[getModId(u)].Select(m => (Mod: m, Update: u)));

            foreach (var (mod, update) in modsToUpdate)
            {
                var originalMod = modsDict[mod.Local.Guid];
                modsDict[mod.Local.Guid] = updateAction(originalMod, update);
            }
        }

        ProcessUpdates(updatesData.SafeToUpdate, u => u.ModId, (m, u) => m.WithSafeToUpdate(u));
        ProcessUpdates(updatesData.Blocked, b => b.ModId, (m, b) => m.WithBlocked(b));
        ProcessUpdates(updatesData.UpToDate, u => u.ModId, (m, u) => m.WithUpToDate(u, linkResolver.ResolveUpToDateLink(m, u)));
        ProcessUpdates(updatesData.Incompatible, i => i.ModId, (m, i) => m.WithIncompatible(i, linkResolver.ResolveIncompatibleLink(m, i)));

        // GitHub API Fallback for missing download links
        foreach (var modId in modsDict.Keys.ToList())
        {
            var mod = modsDict[modId];
            var sourceUrl = !string.IsNullOrWhiteSpace(mod.Api.ApiSourceCodeUrl)
                ? mod.Api.ApiSourceCodeUrl
                : mod.Local.Url;

            if (string.IsNullOrWhiteSpace(mod.Update.DownloadLink) &&
                !string.IsNullOrWhiteSpace(sourceUrl))
            {
                var assetUrl = await gitHubReleaseClient.TryGetLatestReleaseAssetUrlAsync(sourceUrl, cancellationToken);
                if (!string.IsNullOrWhiteSpace(assetUrl))
                {
                    modsDict[modId] = mod with
                    {
                        Update = mod.Update with { DownloadLink = assetUrl }
                    };
                }
            }
        }

        // Fix duplicate mod statuses: if a mod is an older duplicate, it cannot be UpToDate.
        foreach (var group in uniqueModsById.Values.Where(g => g.Count > 1))
        {
            foreach (var mod in group)
            {
                modsDict[mod.Local.Guid] = modsDict[mod.Local.Guid] with { IsDuplicate = true };
            }

            var highestMod = modsDict[group[0].Local.Guid];
            var latestVer = highestMod.Update.LatestVersion ?? highestMod.Local.LocalVersion;

            for (int i = 1; i < group.Count; i++)
            {
                var olderMod = modsDict[group[i].Local.Guid];

                var inheritedStatus = highestMod.Update.UpdateStatus;

                // If it's a duplicate, we only mark it as UpdateAvailable if its version is strictly older than the highest installed version.
                // Otherwise, if they are identical versions, it should just remain whatever the highest mod's status is (e.g. UpToDate).
                var isStrictlyOlder = !string.Equals(olderMod.Local.LocalVersion, highestMod.Local.LocalVersion, StringComparison.OrdinalIgnoreCase);

                if (isStrictlyOlder && (inheritedStatus == UpdateStatus.UpToDate || inheritedStatus == UpdateStatus.Unknown))
                {
                    inheritedStatus = UpdateStatus.UpdateAvailable;
                }

                modsDict[group[i].Local.Guid] = olderMod with
                {
                    Update = olderMod.Update with
                    {
                        UpdateStatus = inheritedStatus,
                        LatestVersion = latestVer,
                        BlockReason = highestMod.Update.BlockReason,
                        BlockingMods = highestMod.Update.BlockingMods,
                        IncompatibilityReason = highestMod.Update.IncompatibilityReason
                    }
                };
            }
        }

        return modsDict.Values.ToList();
    }
}



