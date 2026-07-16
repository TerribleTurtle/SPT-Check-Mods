using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using SemanticVersioning;

namespace CheckModsExtended.Tests.Services;

/// <summary>
/// Tests for <see cref="UpdateCheckService"/> using GitHub releases.
/// </summary>
public sealed class UpdateCheckServiceTests
{
    private static readonly SemanticVersioning.Version SptVersion = new("3.0.0");

    private static UpdateCheckService CreateService(FakeGitHubReleaseClient githubClient)
    {
        return new UpdateCheckService(
            githubClient,
            NullLogger<UpdateCheckService>.Instance
        );
    }

    [Fact]
    public async Task CheckAsync_reports_unavailable_when_github_returns_null()
    {
        var githubClient = new FakeGitHubReleaseClient
        {
            OnGetLatestReleaseVersionAsync = (_, _) => (null, null)
        };

        var result = await CreateService(githubClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task CheckAsync_reports_update_available_when_newer_version_found()
    {
        // current version is statically read from VersionInfo.SemVer which is e.g. "1.0.0"
        var currentVersion = CheckModsExtended.Utils.VersionInfo.SemVer;
        var newerVersionStr = "99.99.99"; // guarantee it's newer
        var downloadLink = "https://github.com/TerribleTurtle/CheckModsExtended/releases/latest";

        var githubClient = new FakeGitHubReleaseClient
        {
            OnGetLatestReleaseVersionAsync = (_, _) => (newerVersionStr, downloadLink)
        };

        var result = await CreateService(githubClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.UpdateAvailable, result.Status);
        Assert.Equal(newerVersionStr, result.LatestVersion);
        Assert.Equal(downloadLink, result.DownloadLink);
    }

    [Fact]
    public async Task CheckAsync_reports_up_to_date_when_same_version_found()
    {
        var currentVersion = CheckModsExtended.Utils.VersionInfo.SemVer;
        
        var githubClient = new FakeGitHubReleaseClient
        {
            OnGetLatestReleaseVersionAsync = (_, _) => ((string?)currentVersion, (string?)"https://link")
        };

        var result = await CreateService(githubClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.UpToDate, result.Status);
    }

    [Fact]
    public async Task CheckAsync_reports_up_to_date_when_older_version_found()
    {
        var githubClient = new FakeGitHubReleaseClient
        {
            OnGetLatestReleaseVersionAsync = (_, _) => ("0.0.1", "https://link")
        };

        var result = await CreateService(githubClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.UpToDate, result.Status);
    }

    [Fact]
    public async Task CheckAsync_reports_unavailable_when_version_is_unparsable()
    {
        var githubClient = new FakeGitHubReleaseClient
        {
            OnGetLatestReleaseVersionAsync = (_, _) => ("invalid-version-string", "https://link")
        };

        var result = await CreateService(githubClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.Unavailable, result.Status);
    }
}
