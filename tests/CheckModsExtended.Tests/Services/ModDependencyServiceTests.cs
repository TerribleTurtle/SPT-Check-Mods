using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using CheckModsExtended.Tests.Fixtures;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckModsExtended.Tests.Services;

/// <summary>
/// Tests for <see cref="ModDependencyService.AnalyzeDependenciesAsync"/>: tree building, missing-dependency and
/// conflict tracking, installed-dependency detection, the circular-dependency guard, and per-mod fetch progress.
/// Dependencies are supplied through an in-memory <see cref="FakeModUpdateClient"/>.
/// </summary>
public sealed class ModDependencyServiceTests
{
    private static ModDependencyService CreateService(FakeModUpdateClient api)
    {
        return new ModDependencyService(api, NullLogger<ModDependencyService>.Instance);
    }

    private static Mod UnmatchedMod(string guid, string name)
    {
        var mod = ModFixture.CreateServerMod(guid, name, "1.0.0", "Author");
        return mod with { Local = mod.Local with { FilePath = $"{name}.dll" } };
    }

    private static Mod MatchedMod(string guid, string name, int apiModId)
    {
        var mod = UnmatchedMod(guid, name);
        mod = mod.WithApiMatch(
            new ModSearchResult(apiModId, null, name, "slug", null, null, 0, null, null, null, null)
        );
        return mod;
    }

    private static Mod MatchedModWithVersion(string guid, string name, int apiModId, string localVersion)
    {
        var mod = ModFixture.CreateServerMod(guid, name, localVersion, "Author");
        mod = mod with { Local = mod.Local with { FilePath = $"{name}.dll" } };
        mod = mod.WithApiMatch(
            new ModSearchResult(apiModId, null, name, "slug", null, null, 0, null, null, null, null)
        );
        return mod;
    }

    /// <summary>A matched mod (installed at 1.0.0) with an available update to <paramref name="latestVersion"/>.</summary>
    private static Mod UpdatableMod(string guid, string name, int apiModId, string latestVersion)
    {
        var mod = MatchedMod(guid, name, apiModId);
        mod = mod.WithSafeToUpdate(
            new SafeToUpdateMod(
                null,
                new ModUpdateVersion(null, apiModId, guid, name, "slug", latestVersion, null, null),
                null
            )
        );
        return mod;
    }

    private static ModDependency Dep(
        string guid,
        string name = "Dep",
        int id = 0,
        string slug = "dep",
        string? version = null,
        bool conflict = false,
        List<ModDependency>? nested = null
    )
    {
        var latest = version is null ? null : new DependencyVersionInfo(1, version, null, null, null);
        return new ModDependency(id, guid, name, slug, latest, conflict, nested);
    }

    [Fact]
    public async Task Returns_all_mods_as_roots_when_none_are_matched()
    {
        // OnGetModDependencies intentionally unset.
        var api = new FakeModUpdateClient();
        var mod = UnmatchedMod("com.x.mod", "Mod");

        var result = await CreateService(api).AnalyzeDependenciesAsync([mod], new HashSet<string>());

        var root = Assert.Single(result.Result.RootMods);
        Assert.Same(mod, root.Mod);
        Assert.Empty(root.Children);
        Assert.Empty(result.Result.MissingDependencies);
        Assert.Empty(result.Result.Conflicts);
    }

    [Fact]
    public async Task Records_a_missing_dependency_with_a_download_link()
    {
        var api = new FakeModUpdateClient
        {
            OnGetModDependencies = _ => new List<ModDependency>
            {
                Dep("com.author.dep", "Dependency", id: 500, slug: "dependency", version: "2.0.0"),
            },
        };

        var result = await CreateService(api)
            .AnalyzeDependenciesAsync([MatchedMod("com.author.main", "Main", 100)], new HashSet<string>());

        var missing = Assert.Single(result.Result.MissingDependencies);
        Assert.Equal("com.author.dep", missing.Guid);
        Assert.Equal("2.0.0", missing.RecommendedVersion);
        Assert.Equal(ForgeUrls.Download(500, "dependency", "2.0.0"), missing.DownloadLink);
    }

    [Fact]
    public async Task Does_not_flag_a_dependency_present_in_the_mod_list()
    {
        var main = MatchedMod("com.author.main", "Main", 100);
        var depMod = MatchedMod("com.author.dep", "Dep", 200);
        var api = new FakeModUpdateClient
        {
            OnGetModDependencies = id =>
                id == "100" ? new List<ModDependency> { Dep("com.author.dep", id: 200) } : new List<ModDependency>(),
        };

        var result = await CreateService(api).AnalyzeDependenciesAsync([main, depMod], new HashSet<string>());
        main = result.UpdatedMods[0];

        Assert.Empty(result.Result.MissingDependencies);
    }

    [Fact]
    public async Task Does_not_flag_a_dependency_listed_in_installed_guids()
    {
        var api = new FakeModUpdateClient
        {
            OnGetModDependencies = _ => new List<ModDependency> { Dep("com.author.dep", id: 200) },
        };

        var result = await CreateService(api)
            .AnalyzeDependenciesAsync(
                [MatchedMod("com.author.main", "Main", 100)],
                new HashSet<string> { "com.author.dep" }
            );

        Assert.Empty(result.Result.MissingDependencies);
    }

    [Fact]
    public async Task Detects_a_version_conflict()
    {
        var api = new FakeModUpdateClient
        {
            OnGetModDependencies = _ => new List<ModDependency>
            {
                Dep("com.author.conf", "Conflicting", id: 500, conflict: true),
            },
        };

        var result = await CreateService(api)
            .AnalyzeDependenciesAsync([MatchedMod("com.author.main", "Main", 100)], new HashSet<string>());

        var conflict = Assert.Single(result.Result.Conflicts);
        Assert.Equal("com.author.conf", conflict.ModGuid);
        Assert.Equal("Conflicting", conflict.ModName);
    }

    [Fact]
    public async Task Guards_against_circular_dependencies()
    {
        // main -> A -> B -> A (circular back-edge).
        var backEdgeToA = Dep("com.a", "A");
        var b = Dep("com.b", "B", nested: [backEdgeToA]);
        var a = Dep("com.a", "A", nested: [b]);
        var api = new FakeModUpdateClient { OnGetModDependencies = _ => new List<ModDependency> { a } };

        var result = await CreateService(api)
            .AnalyzeDependenciesAsync([MatchedMod("com.main", "Main", 100)], new HashSet<string>());

        var root = Assert.Single(result.Result.RootMods);
        var nodeA = Assert.Single(root.Children);
        Assert.Equal("com.a", nodeA.DependencyInfo?.Guid);
        var nodeB = Assert.Single(nodeA.Children);
        Assert.Equal("com.b", nodeB.DependencyInfo?.Guid);
        Assert.Empty(nodeB.Children); // the circular back-edge to com.a was pruned
    }

    [Fact]
    public async Task Allows_diamond_dependencies()
    {
        // main -> A -> D
        // main -> B -> D
        var d = Dep("com.d", "D");
        var a = Dep("com.a", "A", nested: [d]);
        var b = Dep("com.b", "B", nested: [d]);
        
        var api = new FakeModUpdateClient { OnGetModDependencies = _ => new List<ModDependency> { a, b } };

        var result = await CreateService(api)
            .AnalyzeDependenciesAsync([MatchedMod("com.main", "Main", 100)], new HashSet<string>());

        var root = Assert.Single(result.Result.RootMods);
        Assert.Equal(2, root.Children.Count);
        
        var nodeA = root.Children.First(c => c.DependencyInfo?.Guid == "com.a");
        var nodeAChild = Assert.Single(nodeA.Children);
        Assert.Equal("com.d", nodeAChild.DependencyInfo?.Guid);
        
        var nodeB = root.Children.First(c => c.DependencyInfo?.Guid == "com.b");
        var nodeBChild = Assert.Single(nodeB.Children);
        Assert.Equal("com.d", nodeBChild.DependencyInfo?.Guid);
    }

    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;

        public SyncProgress(Action<T> handler)
        {
            _handler = handler;
        }

        public void Report(T value)
        {
            _handler(value);
        }
    }

    [Fact]
    public async Task Invokes_progress_once_per_unique_matched_mod()
    {
        // Two mods sharing one ApiModId should trigger a single dependency fetch.
        var m1 = MatchedMod("com.a.one", "One", 100);
        var m2 = MatchedMod("com.a.two", "Two", 100);
        var calls = new List<int>();
        var api = new FakeModUpdateClient { OnGetModDependencies = _ => new List<ModDependency>() };

        await CreateService(api)
            .AnalyzeDependenciesAsync(
                [m1, m2],
                new HashSet<string>(),
                new SyncProgress<int>(fetched => calls.Add(fetched))
            );

        Assert.Equal(1, Assert.Single(calls));
    }

    [Fact]
    public async Task Treats_a_dependency_fetch_error_as_no_dependencies()
    {
        var api = new FakeModUpdateClient { OnGetModDependencies = _ => new ApiError("boom") };

        var result = await CreateService(api)
            .AnalyzeDependenciesAsync([MatchedMod("com.main", "Main", 100)], new HashSet<string>());

        var root = Assert.Single(result.Result.RootMods);
        Assert.Empty(root.Children);
        Assert.Empty(result.Result.MissingDependencies);
    }

    [Fact]
    public async Task Update_reports_a_newly_required_missing_dependency_with_a_download_link()
    {
        var main = UpdatableMod("com.author.main", "Main", 100, "2.0.0");
        var api = new FakeModUpdateClient
        {
            OnGetModDependenciesVersioned = key =>
                key switch
                {
                    ("100", "2.0.0") => new List<ModDependency>
                    {
                        Dep("com.author.dep", "Dependency", id: 500, slug: "dependency", version: "3.0.0"),
                    },
                    _ => new List<ModDependency>(),
                },
        };

        main = (await CreateService(api).AnalyzeDependenciesAsync([main], new HashSet<string>())).UpdatedMods.First(m =>
            m.Local.Guid == "com.author.main"
        );

        var delta = main.Update.UpdateDependencyChanges;
        Assert.NotNull(delta);
        var added = Assert.Single(delta.Added);
        Assert.Equal("com.author.dep", added.Guid);
        Assert.Equal(DependencyInstallState.NotInstalled, added.InstallState);
        Assert.Equal("3.0.0", added.RecommendedVersion);
        Assert.Equal(ForgeUrls.Download(500, "dependency", "3.0.0"), added.DownloadLink);
        Assert.Empty(delta.Removed);
    }

    [Fact]
    public async Task Update_marks_an_installed_adequate_dependency_as_satisfied()
    {
        var main = UpdatableMod("com.author.main", "Main", 100, "2.0.0");
        var dep = MatchedModWithVersion("com.author.dep", "Dep", 500, "3.0.0");
        var api = new FakeModUpdateClient
        {
            OnGetModDependenciesVersioned = key =>
                key switch
                {
                    ("100", "2.0.0") => new List<ModDependency> { Dep("com.author.dep", id: 500, version: "3.0.0") },
                    _ => new List<ModDependency>(),
                },
        };

        main = (
            await CreateService(api).AnalyzeDependenciesAsync([main, dep], new HashSet<string>())
        ).UpdatedMods.First(m => m.Local.Guid == "com.author.main");

        var added = Assert.Single(main.Update.UpdateDependencyChanges!.Added);
        Assert.Equal(DependencyInstallState.InstalledOk, added.InstallState);
        Assert.Equal("3.0.0", added.InstalledVersion);
    }

    [Fact]
    public async Task Update_flags_an_installed_dependency_that_is_out_of_date()
    {
        var main = UpdatableMod("com.author.main", "Main", 100, "2.0.0");
        var dep = MatchedModWithVersion("com.author.dep", "Dep", 500, "2.0.0");
        var api = new FakeModUpdateClient
        {
            OnGetModDependenciesVersioned = key =>
                key switch
                {
                    ("100", "2.0.0") => new List<ModDependency> { Dep("com.author.dep", id: 500, version: "3.0.0") },
                    _ => new List<ModDependency>(),
                },
        };

        main = (
            await CreateService(api).AnalyzeDependenciesAsync([main, dep], new HashSet<string>())
        ).UpdatedMods.First(m => m.Local.Guid == "com.author.main");

        var added = Assert.Single(main.Update.UpdateDependencyChanges!.Added);
        Assert.Equal(DependencyInstallState.InstalledOutdated, added.InstallState);
        Assert.Equal("2.0.0", added.InstalledVersion);
        Assert.Equal("3.0.0", added.RecommendedVersion);
    }

    [Fact]
    public async Task Update_detects_a_transitively_added_dependency()
    {
        var main = UpdatableMod("com.author.main", "Main", 100, "2.0.0");
        var nested = Dep("com.b", "B", id: 601, version: "1.0.0");
        var api = new FakeModUpdateClient
        {
            OnGetModDependenciesVersioned = key =>
                key switch
                {
                    ("100", "2.0.0") => new List<ModDependency>
                    {
                        Dep("com.a", "A", id: 600, version: "1.0.0", nested: [nested]),
                    },
                    _ => new List<ModDependency>(),
                },
        };

        main = (await CreateService(api).AnalyzeDependenciesAsync([main], new HashSet<string>())).UpdatedMods.First(m =>
            m.Local.Guid == "com.author.main"
        );

        var added = main.Update.UpdateDependencyChanges!.Added;
        Assert.Equal(2, added.Count);
        Assert.Contains(added, c => c.Guid == "com.a");
        Assert.Contains(added, c => c.Guid == "com.b");
    }

    [Fact]
    public async Task Update_reports_a_no_longer_required_dependency()
    {
        var main = UpdatableMod("com.author.main", "Main", 100, "2.0.0");
        var api = new FakeModUpdateClient
        {
            OnGetModDependenciesVersioned = key =>
                key switch
                {
                    ("100", "1.0.0") => new List<ModDependency> { Dep("com.old", "Old", id: 700, version: "1.0.0") },
                    _ => new List<ModDependency>(),
                },
        };

        main = (await CreateService(api).AnalyzeDependenciesAsync([main], new HashSet<string>())).UpdatedMods.First(m =>
            m.Local.Guid == "com.author.main"
        );

        var removed = Assert.Single(main.Update.UpdateDependencyChanges!.Removed);
        Assert.Equal("com.old", removed.Guid);
        Assert.Empty(main.Update.UpdateDependencyChanges.Added);
    }

    [Fact]
    public async Task Does_not_attach_dependency_changes_when_no_update_is_available()
    {
        var main = MatchedMod("com.author.main", "Main", 100);
        var api = new FakeModUpdateClient { OnGetModDependencies = _ => new List<ModDependency>() };

        await CreateService(api).AnalyzeDependenciesAsync([main], new HashSet<string>());

        Assert.Null(main.Update.UpdateDependencyChanges);
    }

    [Fact]
    public async Task A_target_version_fetch_error_leaves_changes_unset_but_keeps_the_current_analysis()
    {
        var main = UpdatableMod("com.author.main", "Main", 100, "2.0.0");
        var api = new FakeModUpdateClient
        {
            OnGetModDependenciesVersioned = key =>
                key switch
                {
                    // Installed version still surfaces its missing dependency...
                    ("100", "1.0.0") => new List<ModDependency>
                    {
                        Dep("com.author.dep", "Dependency", id: 500, slug: "dependency", version: "2.0.0"),
                    },
                    // ...but the proposed-version fetch fails.
                    ("100", "2.0.0") => new ApiError("boom"),
                    _ => new List<ModDependency>(),
                },
        };

        var result = await CreateService(api).AnalyzeDependenciesAsync([main], new HashSet<string>());
        main = result.UpdatedMods[0];
        main = result.UpdatedMods[0];

        Assert.Null(main.Update.UpdateDependencyChanges);
        Assert.Single(result.Result.MissingDependencies);
    }

    [Fact]
    public async Task Update_surfaces_a_conflicting_new_dependency()
    {
        var main = UpdatableMod("com.author.main", "Main", 100, "2.0.0");
        var api = new FakeModUpdateClient
        {
            OnGetModDependenciesVersioned = key =>
                key switch
                {
                    ("100", "2.0.0") => new List<ModDependency>
                    {
                        Dep("com.conf", "Conflicting", id: 800, version: "1.0.0", conflict: true),
                    },
                    _ => new List<ModDependency>(),
                },
        };

        main = (await CreateService(api).AnalyzeDependenciesAsync([main], new HashSet<string>())).UpdatedMods.First(m =>
            m.Local.Guid == "com.author.main"
        );

        var added = Assert.Single(main.Update.UpdateDependencyChanges!.Added);
        Assert.True(added.Conflict);
    }
}
