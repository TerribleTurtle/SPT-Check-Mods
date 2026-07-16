using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using SemanticVersioning;
using SPTarkov.DI.Annotations;
using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services;

/// <summary>
/// Checks whether a newer version of Check Mods is available on GitHub.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class UpdateCheckService(
    IGitHubReleaseClient githubClient,
    ILogger<UpdateCheckService> logger
) : IUpdateCheckService
{
    private const string GitHubOwner = "TerribleTurtle";
    private const string GitHubRepo = "CheckModsExtended";

    /// <inheritdoc />
    public async Task<CheckModsExtendedUpdateResult> CheckAsync(
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var currentVersion = VersionInfo.SemVer;

        logger.LogDebug(
            "Checking for Check Mods updates (GitHub {Owner}/{Repo}, current version {Version})",
            GitHubOwner,
            GitHubRepo,
            currentVersion
        );

        var (latestVersionStr, downloadLink) = await githubClient.GetLatestReleaseVersionAsync(
            GitHubOwner,
            GitHubRepo,
            cancellationToken
        );

        if (string.IsNullOrWhiteSpace(latestVersionStr))
        {
            logger.LogDebug("Check Mods update check failed: Could not fetch latest release from GitHub");
            return new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.Unavailable, currentVersion);
        }

        if (!SemVer.TryParse(currentVersion, "UpdateCheckService").TryPickT0(out var currentSemVer, out _) ||
            !SemVer.TryParse(latestVersionStr, "UpdateCheckService").TryPickT0(out var latestSemVer, out _))
        {
            logger.LogDebug("Check Mods update check failed: Could not parse versions. Current: {Current}, Latest: {Latest}", currentVersion, latestVersionStr);
            return new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.Unavailable, currentVersion);
        }

        if (latestSemVer > currentSemVer)
        {
            return new CheckModsExtendedUpdateResult(
                CheckModsExtendedUpdateStatus.UpdateAvailable,
                currentVersion,
                latestVersionStr,
                downloadLink
            );
        }

        return new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.UpToDate, currentVersion);
    }
}
