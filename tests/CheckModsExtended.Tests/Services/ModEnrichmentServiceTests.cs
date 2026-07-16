using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using SemanticVersioning;
using Xunit;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Services;

public sealed class ModEnrichmentServiceTests
{
    private readonly FakeModUpdateClient _forgeApiService = new();
    private readonly FakeGitHubReleaseClient _gitHubClient = new();
    private readonly FakeLogger<ModEnrichmentService> _logger = new();
    private readonly ModEnrichmentService _service;
    private readonly Version _sptVersion = Version.Parse("3.9.0");

    public ModEnrichmentServiceTests()
    {
        _service = new ModEnrichmentService(_forgeApiService, _gitHubClient, _logger, new CheckModsExtended.Services.ModLinkResolverService());
    }

    [Fact]
    public async Task Skips_unmatched_mods()
    {
        // Arrange
        var unmatchedMod = new Mod
        {
            Local = new CheckModsExtended.Models.LocalModIdentity
            {
                Guid = "com.test.mod",
                FilePath = "test.dll",
                IsServerMod = true,
                LocalName = "Test Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
            },
        };

        // Act
        unmatchedMod = (await _service.EnrichAllWithVersionDataAsync([unmatchedMod], _sptVersion))[0];

        // Assert
        Assert.Contains("No matched mods to enrich", _logger.LoggedMessages);
        Assert.Null(unmatchedMod.Update.LatestVersion);
    }

    [Fact]
    public async Task Handles_api_error_gracefully()
    {
        // Arrange
        var matchedMod = new Mod
        {
            Local = new CheckModsExtended.Models.LocalModIdentity
            {
                Guid = "com.test.mod",
                FilePath = "test.dll",
                IsServerMod = true,
                LocalName = "Test Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
            },
        };
        matchedMod = matchedMod.WithApiMatch(
            new ModSearchResult(123, null, "Test Mod", "test-mod", null, null, 0, null, null, null, null)
        );

        _forgeApiService.OnGetModUpdates = () => new ApiError("Failed to fetch");

        // Act
        matchedMod = (await _service.EnrichAllWithVersionDataAsync([matchedMod], _sptVersion))[0];

        // Assert
        Assert.Null(matchedMod.Update.LatestVersion);
        Assert.Equal(UpdateStatus.Unknown, matchedMod.Update.UpdateStatus);
    }

    [Fact]
    public async Task Maps_safe_to_update_payload_correctly()
    {
        // Arrange
        var matchedMod = new Mod
        {
            Local = new CheckModsExtended.Models.LocalModIdentity
            {
                Guid = "com.test.mod",
                FilePath = "test.dll",
                IsServerMod = true,
                LocalName = "Test Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
            },
        };
        matchedMod = matchedMod.WithApiMatch(
            new ModSearchResult(123, null, "Test Mod", "test-mod", null, null, 0, null, null, null, null)
        );

        _forgeApiService.OnGetModUpdates = () =>
            new ModUpdatesData(
                SafeToUpdate:
                [
                    new SafeToUpdateMod(
                        CurrentVersion: new ModUpdateVersion(10, 123, null, null, null, "1.0.0", null, null),
                        RecommendedVersion: new ModUpdateVersion(
                            11,
                            123,
                            null,
                            null,
                            null,
                            "2.0.0",
                            "http://download",
                            null
                        ),
                        UpdateReason: null
                    ),
                ],
                Blocked: null,
                UpToDate: null,
                Incompatible: null
            );

        // Act
        matchedMod = (await _service.EnrichAllWithVersionDataAsync([matchedMod], _sptVersion))[0];

        // Assert
        Assert.Equal("2.0.0", matchedMod.Update.LatestVersion);
        Assert.Equal("http://download", matchedMod.Update.DownloadLink);
        Assert.Equal(UpdateStatus.UpdateAvailable, matchedMod.Update.UpdateStatus);
    }
    [Fact]
    public async Task Falls_back_to_local_url_when_api_source_code_url_is_missing()
    {
        // Arrange
        var matchedMod = new Mod
        {
            Local = new CheckModsExtended.Models.LocalModIdentity
            {
                Guid = "com.test.mod",
                FilePath = "test.dll",
                IsServerMod = true,
                LocalName = "Test Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                Url = "https://github.com/author/testmod"
            },
        };
        matchedMod = matchedMod.WithApiMatch(
            new ModSearchResult(123, null, "Test Mod", "test-mod", null, null, 0, null, null, null, null)
        );

        _forgeApiService.OnGetModUpdates = () =>
            new ModUpdatesData(null, null, [new UpToDateMod(null, 123, null, null, "1.0.0", null)], null);

        _gitHubClient.OnTryGetLatestReleaseAssetUrlAsync = _ => "https://github.com/author/testmod/releases/download/v1.0.0/testmod.zip";

        // Act
        var result = (await _service.EnrichAllWithVersionDataAsync([matchedMod], _sptVersion))[0];

        // Assert
        Assert.Equal("https://github.com/author/testmod/releases/download/v1.0.0/testmod.zip", result.Update.DownloadLink);
    }

    [Fact]
    public async Task Gracefully_handles_invalid_fallback_url()
    {
        // Arrange
        var matchedMod = new Mod
        {
            Local = new CheckModsExtended.Models.LocalModIdentity
            {
                Guid = "com.test.mod",
                FilePath = "test.dll",
                IsServerMod = true,
                LocalName = "Test Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                Url = "not a valid url format!!"
            },
        };
        matchedMod = matchedMod.WithApiMatch(
            new ModSearchResult(123, null, "Test Mod", "test-mod", null, null, 0, null, null, null, null)
        );

        _forgeApiService.OnGetModUpdates = () =>
            new ModUpdatesData(null, null, [new UpToDateMod(null, 123, null, null, "1.0.0", null)], null);

        // TryGetLatestReleaseAssetUrlAsync returns null when the URL is invalid.
        _gitHubClient.OnTryGetLatestReleaseAssetUrlAsync = _ => null;

        // Act
        var result = (await _service.EnrichAllWithVersionDataAsync([matchedMod], _sptVersion))[0];

        // Assert
        Assert.Null(result.Update.DownloadLink);
    }
}


