using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Options;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Default <see cref="IIgnoredUpdateWorkflow"/>. Presents the currently-flagged updates in a checklist (already-ignored
/// ones pre-checked), rewrites the visible decisions while preserving ignores for mods not evaluated this run, and
/// offers to contribute any entries the community list doesn't already have as a GitHub issue.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class IgnoredUpdateWorkflow(
    IIgnoredUpdateStore store,
    IModCheckReporter reporter,
    IBrowserLauncher browserLauncher,
    IRemoteIgnoreFileClient remoteIgnoreFileClient,
    IOptions<LoggingOptions> loggingOptions
) : IIgnoredUpdateWorkflow
{
    /// <inheritdoc />
    public async Task<EndOfRunChoice> RunAsync(IReadOnlyList<Mod>? mods, CancellationToken cancellationToken = default)
    {
        // One row per Forge mod id (paired server/client mods share an id and a single table row).
        var candidates = (mods ?? [])
            .Where(m => m.Update.UpdateStatus == UpdateStatus.UpdateAvailable && m.Api.ApiModId != null)
            .GroupBy(m => m.Api.ApiModId!.Value)
            .Select(g => g.First())
            .ToList();

        // The mods actually shown as "Updates available"; dismissed false positives are excluded.
        var openable = candidates.Where(m => !m.Update.UpdateSuppressed).ToList();

        // The end-of-run menu loops until the user chooses to close. The counts reflect this run's results and don't
        // change as ignores are edited.
        reporter.ApplicationFooter(VersionInfo.SemVer, VersionInfo.GitHash, loggingOptions.Value.LogFilePath);

        while (true)
        {
            var choice = reporter.PromptEndOfRun(openable.Count, canManageIgnoredUpdates: candidates.Count > 0);

            switch (choice)
            {
                case EndOfRunChoice.OpenUpdatePages:
                    OpenUpdatePages(openable);
                    break;

                case EndOfRunChoice.ManageIgnoredUpdates:
                    await ManageIgnoredUpdatesAsync(mods!, candidates, cancellationToken);
                    break;

                case EndOfRunChoice.Rescan:
                case EndOfRunChoice.LaunchWebGui:
                case EndOfRunChoice.Exit:
                    return choice;

                default:
                    return EndOfRunChoice.Exit;
            }
        }
    }

    /// <summary>
    /// Opens each updatable mod's Forge page in the browser, then reports how many opened.
    /// </summary>
    private void OpenUpdatePages(IReadOnlyList<Mod> openable)
    {
        var urls = BuildUpdatePageUrls(openable);

        var opened = 0;
        foreach (var url in urls)
        {
            if (browserLauncher.TryOpenUrl(url).IsT0)
            {
                opened++;
            }
        }

        reporter.UpdatePagesOpened(opened, urls.Count);
    }

    /// <summary>
    /// Builds the deduplicated list of Forge mod-page URLs to open for the given updatable mods. Prefers the mod's
    /// known detail URL, falling back to one constructed from its Forge id and slug.
    /// </summary>
    internal static IReadOnlyList<string> BuildUpdatePageUrls(IReadOnlyList<Mod> openable)
    {
        var urls = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in openable)
        {
            var url =
                !string.IsNullOrWhiteSpace(mod.Api.ApiUrl) ? mod.Api.ApiUrl
                : mod.Api.ApiModId.HasValue ? ForgeUrls.ModPage(mod.Api.ApiModId.Value, mod.Api.ApiSlug)
                : null;

            if (!string.IsNullOrWhiteSpace(url) && seen.Add(url))
            {
                urls.Add(url);
            }
        }

        return urls;
    }

    /// <summary>
    /// Runs the ignore-management checklist for the available updates and persists the result, then offers to
    /// contribute the just-confirmed ignores.
    /// </summary>
    private async Task ManageIgnoredUpdatesAsync(
        IReadOnlyList<Mod> mods,
        IReadOnlyList<Mod> candidates,
        CancellationToken cancellationToken
    )
    {
        var preIgnoredIds = candidates
            .Where(m => m.Update.UpdateSuppressed)
            .Select(m => m.Api.ApiModId!.Value)
            .ToHashSet();
        var selected = reporter.SelectUpdatesToIgnore(candidates, preIgnoredIds);
        var chosen = selected.Select(ToIgnoredUpdate).ToList();

        await PersistSelectionAsync(mods, chosen, cancellationToken);

        await OfferReportAsync(chosen, cancellationToken);
    }

    private async Task PersistSelectionAsync(
        IReadOnlyList<Mod> mods,
        IReadOnlyList<IgnoredUpdate> chosen,
        CancellationToken cancellationToken
    )
    {
        var evaluatedIds = mods.Where(m => m.IsMatched).Select(m => m.Api.ApiModId!.Value).ToHashSet();
        var newSet = BuildNewSet(await store.LoadAsync(cancellationToken), evaluatedIds, chosen);
        await store.SaveAsync(newSet, cancellationToken);
    }

    /// <summary>
    /// Offers to contribute the just-confirmed ignores as a templated GitHub issue, opening the user's browser to a
    /// pre-filled new-issue form when they accept. Only entries the community list doesn't already have are offered.
    /// </summary>
    /// <param name="chosen">The entries the user confirmed as ignored this run.</param>
    /// <param name="cancellationToken">Token to cancel the community-list fetch.</param>
    private async Task OfferReportAsync(IReadOnlyList<IgnoredUpdate> chosen, CancellationToken cancellationToken)
    {
        if (chosen.Count == 0)
        {
            return;
        }

        var community = await FetchCommunityIgnoresAsync(cancellationToken);
        var reportable = SelectReportableEntries(chosen, community);

        // Nothing to contribute.
        if (reportable.Count == 0)
        {
            return;
        }

        if (!reporter.PromptReportIgnores())
        {
            return;
        }

        var url = IgnoreReportUrl.Build(reportable, out var prefilled);
        var opened = browserLauncher.TryOpenUrl(url).IsT0;
        reporter.IgnoreReportOpened(url, opened, prefilled);
    }

    /// <summary>
    /// Fetches the community (remote) ignore list for comparison only, returning an empty list when it isn't configured
    /// or can't be fetched.
    /// </summary>
    private async Task<IReadOnlyList<IgnoredUpdate>> FetchCommunityIgnoresAsync(CancellationToken cancellationToken)
    {
        if (!remoteIgnoreFileClient.IsConfigured)
        {
            return [];
        }

        return await remoteIgnoreFileClient.FetchAsync(cancellationToken) ?? [];
    }

    /// <summary>
    /// Returns the entries worth contributing: those from <paramref name="chosen"/> whose key isn't already present in
    /// the <paramref name="community"/> list. Matching uses <see cref="IgnoredUpdate.Key"/> compared case-insensitively.
    /// An empty result means the community already covers everything chosen.
    /// </summary>
    internal static IReadOnlyList<IgnoredUpdate> SelectReportableEntries(
        IReadOnlyList<IgnoredUpdate> chosen,
        IReadOnlyList<IgnoredUpdate> community
    )
    {
        var communityKeys = community.Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return chosen.Where(e => !communityKeys.Contains(e.Key)).ToList();
    }

    /// <summary>
    /// Builds the new ignore set. Keep existing entries for mods that weren't evaluated this run, then add this run's
    /// selected entries.
    /// </summary>
    internal static List<IgnoredUpdate> BuildNewSet(
        IReadOnlyList<IgnoredUpdate> existing,
        ISet<int> evaluatedApiModIds,
        IReadOnlyList<IgnoredUpdate> selected
    )
    {
        var result = existing.Where(e => !evaluatedApiModIds.Contains(e.ApiModId)).ToList();
        var keys = result.Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in selected)
        {
            if (keys.Add(entry.Key))
            {
                result.Add(entry);
            }
        }

        return result;
    }

    private static IgnoredUpdate ToIgnoredUpdate(Mod mod)
    {
        return new IgnoredUpdate(
            ApiModId: mod.Api.ApiModId!.Value,
            LocalVersion: mod.Local.LocalVersion,
            IgnoredLatestVersion: mod.Update.LatestVersion!,
            Name: mod.DisplayName,
            Guid: string.IsNullOrWhiteSpace(mod.Local.Guid) ? null : mod.Local.Guid,
            Source: IgnoreSource.User,
            DismissedUtc: DateTimeOffset.UtcNow
        );
    }
}
