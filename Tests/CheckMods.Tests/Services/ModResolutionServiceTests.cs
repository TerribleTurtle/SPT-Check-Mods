using System.Collections.Generic;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services;
using CheckMods.Tests; // To get the right FakeForgeApiService
using SemanticVersioning;
using Xunit;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Services;

public sealed class ModResolutionServiceTests
{
    private readonly FakeForgeApiService _forgeApiService = new();
    private readonly ModResolutionService _sut;

    public ModResolutionServiceTests()
    {
        _sut = new ModResolutionService(new ModLookupStrategy(_forgeApiService));
    }

    [Fact]
    public async Task FetchSourceCodeUrlsForModsAsync_matches_by_guid_if_present()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "exact-guid",
                FilePath = "test",
                LocalName = "TestMod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
        };

        var apiResult = new ModSearchResult(
            1,
            null,
            "Test Mod",
            "test-mod",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            []
        );
        _forgeApiService.OnGetModByGuid = _ => apiResult;

        // Act
        mod = (await _sut.FetchSourceCodeUrlsForModsAsync([mod], sptVersion))[0];

        // Assert
        Assert.True(mod.IsMatched);
        Assert.Equal(1, mod.Api.ApiModId);
    }

    [Fact]
    public async Task FetchSourceCodeUrlsForModsAsync_falls_back_to_name_search_and_exact_match()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "unknown",
                FilePath = "test",
                LocalName = "Exact Name Match",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
        };

        // Guid lookup fails
        _forgeApiService.OnGetModByGuid = _ => new NotFound();

        var apiResult = new ModSearchResult(
            2,
            null,
            "Exact Name Match",
            "exact",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            []
        );
        _forgeApiService.OnSearch = _ => new List<ModSearchResult> { apiResult };

        // Act
        mod = (await _sut.FetchSourceCodeUrlsForModsAsync([mod], sptVersion))[0];

        // Assert
        Assert.True(mod.IsMatched);
        Assert.Equal(2, mod.Api.ApiModId);
    }

    [Fact]
    public async Task FetchSourceCodeUrlsForModsAsync_falls_back_to_fuzzy_match_above_threshold()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "unknown",
                FilePath = "test",
                LocalName = "Some Plugin Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
        };

        var apiResult = new ModSearchResult(
            3,
            null,
            "Some Plugin Mdo",
            "some",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            []
        );
        _forgeApiService.OnSearch = _ => new List<ModSearchResult> { apiResult };

        // Act
        mod = (await _sut.FetchSourceCodeUrlsForModsAsync([mod], sptVersion))[0];

        // Assert
        Assert.True(mod.IsMatched);
        Assert.Equal(3, mod.Api.ApiModId);
    }

    [Fact]
    public async Task FetchSourceCodeUrlsForModsAsync_rejects_fuzzy_match_below_threshold()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "unknown",
                FilePath = "test",
                LocalName = "Short",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
        };

        var apiResult = new ModSearchResult(
            4,
            null,
            "Completely Unrelated Mod Name Here",
            "unrelated",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            []
        );
        _forgeApiService.OnSearch = _ => new List<ModSearchResult> { apiResult };

        // Act
        mod = (await _sut.FetchSourceCodeUrlsForModsAsync([mod], sptVersion))[0];

        // Assert
        Assert.False(mod.IsMatched);
    }

    [Fact]
    public async Task FetchSourceCodeUrlsForModsAsync_extracts_url_from_NoCompatibleVersion_fallback()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "outdated-guid",
                FilePath = "test",
                LocalName = "OutdatedMod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
        };

        var apiResult = new ModSearchResult(
            5,
            null,
            "Outdated Mod",
            "outdated-mod",
            null,
            null,
            0,
            [new SourceCodeLink("https://github.com/outdated/mod", null)],
            null,
            new ModAuthor(1, "Author", null),
            []
        );
        _forgeApiService.OnGetModByGuid = _ => new NoCompatibleVersion(apiResult);

        // Act
        mod = (await _sut.FetchSourceCodeUrlsForModsAsync([mod], sptVersion))[0];

        // Assert
        Assert.True(mod.IsMatched);
        Assert.Equal(5, mod.Api.ApiModId);
        Assert.Equal("https://github.com/outdated/mod", mod.Api.ApiSourceCodeUrl);
    }
}
