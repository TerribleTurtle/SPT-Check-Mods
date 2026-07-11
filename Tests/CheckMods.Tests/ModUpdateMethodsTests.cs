using CheckMods.Models;

namespace CheckMods.Tests;

/// <summary>
/// Tests for the <see cref="Mod"/> enrichment methods and computed display properties - the transforms that carry
/// Forge API data onto the mod model. Pure logic with no I/O.
/// </summary>
public sealed class ModUpdateMethodsTests
{
    private static Mod NewMod(string name = "Cool Mod", string author = "Author", string version = "1.0.0")
    {
        return new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity {
                Guid = "com.author.coolmod",
                FilePath = $"mods/{name}.dll",
                IsServerMod = true,
                LocalName = name,
                LocalAuthor = author,
                LocalVersion = version,
            }
        };
    }

    private static ModVersion ApiVersion(string version, string constraint = ">=1.0.0", string? link = null)
    {
        return new ModVersion(
            Id: 1,
            HubId: null,
            Version: version,
            Description: null,
            Link: link,
            SptVersionConstraint: constraint,
            VirusTotalLink: null,
            Downloads: 0,
            PublishedAt: null,
            CreatedAt: null,
            UpdatedAt: null
        );
    }

    private static ModSearchResult ApiResult(
        int id = 2471,
        string name = "Cool Mod",
        ModAuthor? owner = null,
        string? detailUrl = "https://forge.sp-tarkov.com/mod/2471/cool-mod",
        List<SourceCodeLink>? sourceLinks = null,
        List<ModVersion>? versions = null
    )
    {
        return new ModSearchResult(
            Id: id,
            HubId: null,
            Name: name,
            Slug: "cool-mod",
            Teaser: null,
            Thumbnail: null,
            Downloads: 0,
            SourceCodeLinks: sourceLinks,
            DetailUrl: detailUrl,
            Owner: owner,
            Versions: versions
        );
    }

    private static ModUpdateVersion UpdateVersion(string version, string? link = null)
    {
        return new ModUpdateVersion(
            Id: null,
            ModId: 2471,
            Guid: "com.author.coolmod",
            Name: "Cool Mod",
            Slug: "cool-mod",
            Version: version,
            Link: link,
            SptVersions: null
        );
    }

    [Fact]
    public void UpdateFromApiMatch_populates_api_fields_and_marks_verified()
    {
        var mod = NewMod();
        var result = ApiResult(
            owner: new ModAuthor(7, "ForgeAuthor", null),
            sourceLinks: [new SourceCodeLink("https://github.com/author/coolmod", "GitHub")],
            versions: [ApiVersion("1.0.0"), ApiVersion("1.1.0")]
        );

        mod.UpdateFromApiMatch(result);

        Assert.Equal(2471, mod.Api.ApiModId);
        Assert.Equal("Cool Mod", mod.Api.ApiName);
        Assert.Equal("ForgeAuthor", mod.Api.ApiAuthor?.Name);
        Assert.Equal("cool-mod", mod.Api.ApiSlug);
        Assert.Equal("https://forge.sp-tarkov.com/mod/2471/cool-mod", mod.Api.ApiUrl);
        Assert.Equal("https://github.com/author/coolmod", mod.Api.ApiSourceCodeUrl);
        Assert.Equal(2, mod.Api.ApiVersions?.Count);
        Assert.Equal(ModStatus.Verified, mod.Status);
        Assert.True(mod.IsMatched);
    }

    [Fact]
    public void UpdateFromApiMatch_handles_null_optionals()
    {
        var mod = NewMod();
        var result = ApiResult(owner: null, detailUrl: null, sourceLinks: null, versions: null);

        mod.UpdateFromApiMatch(result);

        Assert.Null(mod.Api.ApiAuthor);
        Assert.Null(mod.Api.ApiUrl);
        Assert.Null(mod.Api.ApiSourceCodeUrl);
        Assert.Null(mod.Api.ApiVersions);
        Assert.Equal(ModStatus.Verified, mod.Status);

        Assert.True(mod.IsMatched);
    }

    [Fact]
    public void MarkUnmatched_resets_status_after_a_match()
    {
        var mod = NewMod();
        mod.UpdateFromApiMatch(ApiResult());
        Assert.True(mod.IsMatched);

        mod.MarkUnmatched();

        Assert.Equal(ModStatus.NoMatch, mod.Status);
        Assert.False(mod.IsMatched);
    }

    [Fact]
    public void UpdateFromSafeToUpdate_sets_version_link_and_status()
    {
        var mod = NewMod();
        var update = new SafeToUpdateMod(
            CurrentVersion: UpdateVersion("1.0.0"),
            RecommendedVersion: UpdateVersion("1.1.0", "https://forge.sp-tarkov.com/mod/download/2471/cool-mod/1.1.0"),
            UpdateReason: "newer_version_available"
        );

        mod.UpdateFromSafeToUpdate(update);

        Assert.Equal("1.1.0", mod.Update.LatestVersion);
        Assert.Equal("https://forge.sp-tarkov.com/mod/download/2471/cool-mod/1.1.0", mod.Update.DownloadLink);
        Assert.Equal(UpdateStatus.UpdateAvailable, mod.Update.UpdateStatus);
    }

    [Fact]
    public void UpdateFromSafeToUpdate_handles_null_recommended_version()
    {
        var mod = NewMod();
        var update = new SafeToUpdateMod(UpdateVersion("1.0.0"), RecommendedVersion: null, UpdateReason: null);

        mod.UpdateFromSafeToUpdate(update);

        Assert.Null(mod.Update.LatestVersion);
        Assert.Null(mod.Update.DownloadLink);
        Assert.Equal(UpdateStatus.UpdateAvailable, mod.Update.UpdateStatus);
    }

    [Fact]
    public void UpdateFromBlocked_sets_fields_and_status()
    {
        var mod = NewMod();
        var blocking = new BlockingModInfo(99, "com.other.mod", "Other Mod", "1.0.0", "<2.0.0", null);
        var blocked = new BlockedUpdateMod(
            CurrentVersion: UpdateVersion("1.0.0"),
            LatestVersion: UpdateVersion("2.0.0"),
            BlockReason: "dependency_constraint_violation",
            BlockingMods: [blocking]
        );

        mod.UpdateFromBlocked(blocked);

        Assert.Equal("2.0.0", mod.Update.LatestVersion);
        Assert.Equal("dependency_constraint_violation", mod.Update.BlockReason);
        Assert.Equal(blocking, Assert.Single(mod.Update.BlockingMods!));
        Assert.Equal(UpdateStatus.UpdateBlocked, mod.Update.UpdateStatus);
    }

    [Fact]
    public void UpdateFromBlocked_handles_null_latest_version()
    {
        var mod = NewMod();
        var blocked = new BlockedUpdateMod(
            UpdateVersion("1.0.0"),
            LatestVersion: null,
            BlockReason: null,
            BlockingMods: null
        );

        mod.UpdateFromBlocked(blocked);

        Assert.Null(mod.Update.LatestVersion);
        Assert.Equal(UpdateStatus.UpdateBlocked, mod.Update.UpdateStatus);
    }

    [Fact]
    public void UpdateFromUpToDate_sets_version_and_status()
    {
        var mod = NewMod();

        mod.UpdateFromUpToDate(new UpToDateMod(null, 2471, "com.author.coolmod", "Cool Mod", "1.0.0", null));

        Assert.Equal("1.0.0", mod.Update.LatestVersion);
        Assert.Equal(UpdateStatus.UpToDate, mod.Update.UpdateStatus);
    }

    [Fact]
    public void UpdateFromIncompatible_sets_reason_and_status()
    {
        var mod = NewMod();

        mod.UpdateFromIncompatible(
            new IncompatibleMod(null, 2471, "com.author.coolmod", "Cool Mod", "1.0.0", "no_version_for_spt", null)
        );

        Assert.Equal("no_version_for_spt", mod.Update.IncompatibilityReason);
        Assert.Equal(UpdateStatus.Incompatible, mod.Update.UpdateStatus);
    }

    [Fact]
    public void SetLocalSptIncompatible_records_reason_and_compatible_version()
    {
        var mod = NewMod();

        mod.SetLocalSptIncompatible("Version 1.0.0 requires SPT ~3.0.0", "1.2.0");

        Assert.True(mod.Update.IsLocalSptIncompatible);
        Assert.Equal("Version 1.0.0 requires SPT ~3.0.0", mod.Update.IncompatibilityReason);
        Assert.Equal("1.2.0", mod.Update.CompatibleVersionString);
    }

    [Fact]
    public void SetLocalSptIncompatible_leaves_compatible_version_null_by_default()
    {
        var mod = NewMod();

        mod.SetLocalSptIncompatible("No compatible version available");

        Assert.True(mod.Update.IsLocalSptIncompatible);
        Assert.Null(mod.Update.CompatibleVersionString);
    }

    [Fact]
    public void IsMatched_is_false_for_a_freshly_scanned_mod()
    {
        Assert.False(NewMod().IsMatched);
    }

    [Fact]
    public void DisplayName_prefers_api_name_then_falls_back_to_local()
    {
        var mod = NewMod(name: "LocalName");
        Assert.Equal("LocalName", mod.DisplayName);

        mod.UpdateFromApiMatch(ApiResult(name: "Forge Name"));
        Assert.Equal("Forge Name", mod.DisplayName);
    }

    [Fact]
    public void DisplayAuthor_falls_back_to_local_when_no_api_author()
    {
        var mod = NewMod(author: "LocalAuthor");
        Assert.Equal("LocalAuthor", mod.DisplayAuthor);

        mod.UpdateFromApiMatch(ApiResult(owner: null));
        Assert.Equal("LocalAuthor", mod.DisplayAuthor);
    }

    [Fact]
    public void DisplayAuthor_prefers_api_author_when_present()
    {
        var mod = NewMod(author: "LocalAuthor");

        mod.UpdateFromApiMatch(ApiResult(owner: new ModAuthor(7, "ForgeAuthor", null)));

        Assert.Equal("ForgeAuthor", mod.DisplayAuthor);
    }

    [Fact]
    public void HasWarnings_reflects_load_warnings()
    {
        var mod = NewMod();
        Assert.False(mod.HasWarnings);

        mod.LoadWarnings.Add("something went wrong");
        Assert.True(mod.HasWarnings);
    }
}






