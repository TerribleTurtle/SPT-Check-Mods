using CheckMods.Models;
using CheckMods.Services;
using CheckMods.Tests.Fakes;
using Xunit;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Services;

public sealed class ModEnrichmentServiceTests
{
    private readonly FakeForgeApiService _forgeApiService = new();
    private readonly FakeLogger<ModEnrichmentService> _logger = new();
    private readonly ModEnrichmentService _service;
    private readonly Version _sptVersion = Version.Parse("3.9.0");

    public ModEnrichmentServiceTests()
    {
        _service = new ModEnrichmentService(_forgeApiService, _logger);
    }

    [Fact]
    public async Task skips_unmatched_mods()
    {
        // Arrange
        var unmatchedMod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
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
    public async Task handles_api_error_gracefully()
    {
        // Arrange
        var matchedMod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
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
    public async Task maps_safe_to_update_payload_correctly()
    {
        // Arrange
        var matchedMod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
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
}
