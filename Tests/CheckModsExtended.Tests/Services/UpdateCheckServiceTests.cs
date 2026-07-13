using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SemanticVersioning;

namespace CheckModsExtended.Tests.Services;

/// <summary>
/// Tests for <see cref="UpdateCheckService"/>: the static <see cref="UpdateCheckService.InterpretUpdates"/> mapper, and
/// the <see cref="UpdateCheckService.CheckAsync"/> flow - in particular the unrecognized-build fallback that selects
/// the latest stable version.
/// </summary>
public sealed class UpdateCheckServiceTests
{
    private const int ModId = 2471;

    private static readonly SemanticVersioning.Version SptVersion = new("3.0.0");

    private static UpdateCheckService CreateService(FakeModSearchClient searchClient, FakeModUpdateClient updateClient)
    {
        return new UpdateCheckService(
            searchClient,
            updateClient,
            Options.Create(new UpdateCheckOptions { ForgeModId = ModId }),
            NullLogger<UpdateCheckService>.Instance
        );
    }

    private static ModVersion ApiModVersion(string version, string? link = null)
    {
        return new ModVersion(
            Id: 1,
            HubId: null,
            Version: version,
            Description: null,
            Link: link,
            SptVersionConstraint: ">=1.0.0",
            VirusTotalLink: null,
            Downloads: 0,
            PublishedAt: null,
            CreatedAt: null,
            UpdatedAt: null
        );
    }

    private static ModSearchResult ModWithVersions(string? detailUrl, params ModVersion[] versions)
    {
        return new ModSearchResult(
            Id: ModId,
            HubId: null,
            Name: "Check Mods",
            Slug: "check-mods",
            Teaser: null,
            Thumbnail: null,
            Downloads: 0,
            SourceCodeLinks: null,
            DetailUrl: detailUrl,
            Owner: null,
            Versions: versions.ToList()
        );
    }

    private static ModUpdateVersion Version(int modId, string version, string? link = null)
    {
        return new ModUpdateVersion(
            Id: null,
            ModId: modId,
            Guid: "com.refringe.CheckModsExtended",
            Name: "Check Mods",
            Slug: "check-mods",
            Version: version,
            Link: link,
            SptVersions: null
        );
    }

    [Fact]
    public void Returns_update_available_when_mod_in_updates()
    {
        var data = new ModUpdatesData(
            SafeToUpdate:
            [
                new SafeToUpdateMod(
                    Version(ModId, "1.0.0"),
                    Version(ModId, "1.0.1", "https://forge.sp-tarkov.com/mod/download/2471/check-mods/1.0.1"),
                    "newer_version_available"
                ),
            ],
            Blocked: null,
            UpToDate: null,
            Incompatible: null
        );

        var result = UpdateCheckService.InterpretUpdates(data, ModId, "1.0.0");

        Assert.NotNull(result);
        Assert.Equal(CheckModsExtendedUpdateStatus.UpdateAvailable, result.Status);
        Assert.Equal("1.0.1", result.LatestVersion);
        Assert.Equal("https://forge.sp-tarkov.com/mod/download/2471/check-mods/1.0.1", result.DownloadLink);
    }

    [Fact]
    public void Returns_up_to_date_when_mod_in_up_to_date()
    {
        var data = new ModUpdatesData(
            SafeToUpdate: null,
            Blocked: null,
            UpToDate: [new UpToDateMod(null, ModId, "com.refringe.CheckModsExtended", "Check Mods", "1.0.1", null)],
            Incompatible: null
        );

        var result = UpdateCheckService.InterpretUpdates(data, ModId, "1.0.1");

        Assert.NotNull(result);
        Assert.Equal(CheckModsExtendedUpdateStatus.UpToDate, result.Status);
    }

    [Fact]
    public void Returns_incompatible_when_mod_in_incompatible()
    {
        var data = new ModUpdatesData(
            SafeToUpdate: null,
            Blocked: null,
            UpToDate: null,
            Incompatible:
            [
                new IncompatibleMod(
                    null,
                    ModId,
                    "com.refringe.CheckModsExtended",
                    "Check Mods",
                    "1.0.1",
                    "no_version_for_spt",
                    null
                ),
            ]
        );

        var result = UpdateCheckService.InterpretUpdates(data, ModId, "1.0.1");

        Assert.NotNull(result);
        Assert.Equal(CheckModsExtendedUpdateStatus.IncompatibleWithSpt, result.Status);
    }

    [Fact]
    public void Returns_null_when_all_categories_empty()
    {
        var data = new ModUpdatesData([], [], [], []);

        var result = UpdateCheckService.InterpretUpdates(data, ModId, "0.0.1");

        Assert.Null(result);
    }

    [Fact]
    public void Returns_null_when_mod_absent_from_populated_categories()
    {
        var data = new ModUpdatesData(
            SafeToUpdate:
            [
                new SafeToUpdateMod(
                    Version(9999, "1.0.0"),
                    Version(9999, "2.0.0", "https://x"),
                    "newer_version_available"
                ),
            ],
            Blocked: null,
            UpToDate: [new UpToDateMod(null, 8888, "com.other", "Another", "1.0.0", null)],
            Incompatible: null
        );

        var result = UpdateCheckService.InterpretUpdates(data, ModId, "1.0.0");

        Assert.Null(result);
    }

    [Fact]
    public async Task Checkasync_reports_update_available_from_the_updates_endpoint()
    {
        var data = new ModUpdatesData(
            SafeToUpdate:
            [
                new SafeToUpdateMod(
                    Version(ModId, "1.0.0"),
                    Version(ModId, "9.9.9", "https://download/9.9.9"),
                    "newer_version_available"
                ),
            ],
            Blocked: null,
            UpToDate: null,
            Incompatible: null
        );
        var searchClient = new FakeModSearchClient();
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => data };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.UpdateAvailable, result.Status);
        Assert.Equal("9.9.9", result.LatestVersion);
    }

    [Fact]
    public async Task Checkasync_reports_unavailable_on_api_error()
    {
        var searchClient = new FakeModSearchClient();
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new ApiError("boom") };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Checkasync_recommends_latest_stable_version_for_an_unrecognized_build()
    {
        var searchClient = new FakeModSearchClient
        {
            OnGetModById = _ =>
                ModWithVersions(
                    "https://detail",
                    ApiModVersion("1.0.0"),
                    ApiModVersion("2.0.0-beta.1"),
                    ApiModVersion("1.2.0")
                ),
        };
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new NotFound() };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.UnrecognizedBuild, result.Status);
        Assert.Equal("1.2.0", result.LatestVersion); // 2.0.0-beta.1 is excluded as a prerelease
    }

    [Fact]
    public async Task Checkasync_orders_unrecognized_versions_by_semver_not_string()
    {
        var searchClient = new FakeModSearchClient
        {
            OnGetModById = _ =>
                ModWithVersions(null, ApiModVersion("1.2.0"), ApiModVersion("1.10.0"), ApiModVersion("1.9.0")),
        };
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new NotFound() };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal("1.10.0", result.LatestVersion);
    }

    [Fact]
    public async Task Checkasync_reports_unavailable_when_only_prereleases_exist()
    {
        var searchClient = new FakeModSearchClient
        {
            OnGetModById = _ => ModWithVersions(null, ApiModVersion("1.0.0-alpha"), ApiModVersion("2.0.0-beta")),
        };
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new NotFound() };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Checkasync_uses_the_version_link_as_download_link_when_present()
    {
        var searchClient = new FakeModSearchClient
        {
            OnGetModById = _ => ModWithVersions("https://detail", ApiModVersion("1.0.0", "https://download/1.0.0")),
        };
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new NotFound() };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal("https://download/1.0.0", result.DownloadLink);
    }

    [Fact]
    public async Task Checkasync_falls_back_to_detail_url_when_the_version_has_no_link()
    {
        var searchClient = new FakeModSearchClient
        {
            OnGetModById = _ => ModWithVersions("https://detail-page", ApiModVersion("1.0.0", link: null)),
        };
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new NotFound() };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal("https://detail-page", result.DownloadLink);
    }

    [Fact]
    public async Task Checkasync_reports_unavailable_when_the_mod_is_not_found()
    {
        var searchClient = new FakeModSearchClient { OnGetModById = _ => new NotFound() };
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new NotFound() };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Checkasync_reports_unavailable_when_the_mod_has_no_versions()
    {
        var searchClient = new FakeModSearchClient { OnGetModById = _ => ModWithVersions(detailUrl: "https://detail") };
        var updateClient = new FakeModUpdateClient { OnGetModUpdates = () => new NotFound() };

        var result = await CreateService(searchClient, updateClient).CheckAsync(SptVersion);

        Assert.Equal(CheckModsExtendedUpdateStatus.Unavailable, result.Status);
    }
}
