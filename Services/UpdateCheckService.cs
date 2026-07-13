using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticVersioning;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Checks whether a newer version of Check Mods is available on the Forge, using the Forge API's mod updates endpoint.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class UpdateCheckService(
    IModSearchClient searchClient,
    IModUpdateClient updateClient,
    IOptions<UpdateCheckOptions> options,
    ILogger<UpdateCheckService> logger
) : IUpdateCheckService
{
    private readonly UpdateCheckOptions _options = options.Value;

    /// <inheritdoc />
    public async Task<CheckModsExtendedUpdateResult> CheckAsync(
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var currentVersion = VersionInfo.SemVer;
        var modId = _options.ForgeModId;

        logger.LogDebug(
            "Checking for Check Mods updates (mod {ModId}, current version {Version}, SPT {SptVersion})",
            modId,
            currentVersion,
            sptVersion
        );

        var updatesResult = await updateClient.GetModUpdatesAsync(
            [(modId, currentVersion)],
            sptVersion,
            cancellationToken
        );

        if (updatesResult.TryPickT0(out var data, out var remainder))
        {
            var interpreted = InterpretUpdates(data, modId, currentVersion);
            if (interpreted is not null)
            {
                return interpreted;
            }

            // The running version isn't a recognized Forge release.
            return await ResolveUnrecognizedAsync(modId, currentVersion, cancellationToken);
        }

        if (remainder.IsT0)
        {
            return await ResolveUnrecognizedAsync(modId, currentVersion, cancellationToken);
        }

        logger.LogDebug("Check Mods update check failed: API error");
        return new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.Unavailable, currentVersion);
    }

    /// <summary>
    /// Maps a categorized mod-updates response to an update result for the given mod, or null when the mod is absent
    /// from every category (the current version couldn't be resolved).
    /// </summary>
    public static CheckModsExtendedUpdateResult? InterpretUpdates(ModUpdatesData data, int modId, string currentVersion)
    {
        var safe = data.SafeToUpdate?.FirstOrDefault(u => u.ModId == modId);
        if (safe?.RecommendedVersion is not null)
        {
            return new CheckModsExtendedUpdateResult(
                CheckModsExtendedUpdateStatus.UpdateAvailable,
                currentVersion,
                safe.RecommendedVersion.Version,
                safe.RecommendedVersion.Link
            );
        }

        if (data.UpToDate?.Any(u => u.ModId == modId) == true)
        {
            return new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.UpToDate, currentVersion);
        }

        var incompatible = data.Incompatible?.FirstOrDefault(i => i.ModId == modId);
        if (incompatible is not null)
        {
            return new CheckModsExtendedUpdateResult(
                CheckModsExtendedUpdateStatus.IncompatibleWithSpt,
                currentVersion,
                incompatible.LatestCompatibleVersion?.Version
            );
        }

        return null;
    }

    /// <summary>
    /// Handles the case where the running version wasn't recognized. If the mod exists on the Forge, suggests the
    /// latest stable version; otherwise reports the check as unavailable.
    /// </summary>
    private async Task<CheckModsExtendedUpdateResult> ResolveUnrecognizedAsync(
        int modId,
        string currentVersion,
        CancellationToken cancellationToken
    )
    {
        var modResult = await searchClient.GetModByIdAsync(modId, cancellationToken);

        if (!modResult.TryPickT0(out var mod, out _) || mod.Versions is not { Count: > 0 })
        {
            // Mod missing/disabled, or no versions to recommend.
            return new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.Unavailable, currentVersion);
        }

        var latestStable = mod
            .Versions.Select(v => (Raw: v, Parsed: SemVer.TryParse(v.Version, "UpdateCheckService").AsT0))
            .Where(x => x.Parsed is not null && string.IsNullOrEmpty(x.Parsed!.PreRelease))
            .OrderByDescending(x => x.Parsed)
            .Select(x => x.Raw)
            .FirstOrDefault();

        if (latestStable is null)
        {
            return new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.Unavailable, currentVersion);
        }

        var downloadLink = !string.IsNullOrWhiteSpace(latestStable.Link) ? latestStable.Link : mod.DetailUrl;
        return new CheckModsExtendedUpdateResult(
            CheckModsExtendedUpdateStatus.UnrecognizedBuild,
            currentVersion,
            latestStable.Version,
            downloadLink
        );
    }
}
