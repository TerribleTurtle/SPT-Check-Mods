using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.UI;
using CheckMods.Tests.Fakes;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace CheckMods.Tests.Services.UI;

[Collection("ConsoleTests")]
public sealed class VersionTableUiRendererTests
{
    [Fact]
    public void version_table_renders_table_with_mods()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new VersionTableUiRenderer(new FakeTextRenderer());

        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "test.mod",
                FilePath = "test.dll",
                IsServerMod = false,
                LocalName = "Test Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
            },
        };
        mod = mod.WithUpToDate(new UpToDateMod(null, 123, null, null, "1.0.0", null));
        mod = mod.WithApiMatch(
            new ModSearchResult(123, null, "Test Mod", "test-mod", null, null, 0, null, null, null, null)
        );

        renderer.VersionTable(new List<Mod> { mod });

        var output = console.Output;
        Assert.Contains("Test Mod", output);
        Assert.Contains("Author", output);
        Assert.Contains("1.0.0", output);
    }

    [Fact]
    public void version_table_renders_update_available()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new VersionTableUiRenderer(new FakeTextRenderer());

        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "test.mod",
                FilePath = "test.dll",
                IsServerMod = false,
                LocalName = "Test Mod Update",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
            },
        };
        mod = mod.WithSafeToUpdate(
            new SafeToUpdateMod(null, new ModUpdateVersion(null, 123, null, null, null, "2.0.0", null, null), null)
        );
        mod = mod.WithApiMatch(
            new ModSearchResult(123, null, "Test Mod Update", "test-mod", null, null, 0, null, null, null, null)
        );

        renderer.VersionTable(new List<Mod> { mod });

        var output = console.Output;
        Assert.Contains("Updates available", output);
        Assert.Contains("Test Mod Update", output);
        Assert.Contains("2.0.0", output);
    }

    [Fact]
    public void version_compatibility_results_renders_incompatible()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new VersionTableUiRenderer(new FakeTextRenderer());

        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "test.mod",
                FilePath = "test.dll",
                IsServerMod = false,
                LocalName = "Incompatible Mod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
            },
        };
        mod = mod.WithLocalSptIncompatible("Requires SPT 4.0.0", "1.1.0");

        renderer.VersionCompatibilityResults(new List<Mod> { mod }, new SemanticVersioning.Version(3, 9, 0));

        var output = console.Output;
        Assert.Contains("Incompatible mods", output);
        Assert.Contains("Incompatible Mod", output);
        Assert.Contains("Requires SPT 4.0.0", output);
    }

    [Fact]
    public void version_table_renders_blocked_and_suppressed_updates_with_dependency_changes()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new VersionTableUiRenderer(new FakeTextRenderer());

        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "test.mod",
                IsServerMod = false,
                LocalName = "Test Mod Update",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                FilePath = "test.dll",
            },
        };

        mod = mod.WithApiMatch(new ModSearchResult(123, null, "Test Mod Update", "test-mod", null, null, 0, null, null, null, null));
        mod = mod.WithSafeToUpdate(new SafeToUpdateMod(
            new ModUpdateVersion(null, 123, null, null, null, "1.0.0", null, null),
            new ModUpdateVersion(null, 123, null, null, null, "2.0.0", null, null),
            "Major update"
        ));

        mod = mod with
        {
            Update = mod.Update with
            {
                UpdateStatus = UpdateStatus.UpdateBlocked,
                BlockReason = "Blocked by dependency",
                BlockingMods = [new BlockingModInfo(123, "dep", "dependency", "1.0.0", "1.0.0", null)],
                UpdateSuppressed = true
            }
        };

        var depDelta = new UpdateDependencyDelta
        {
            Added = new List<DependencyChange>
            {
                new DependencyChange
                {
                    Name = "New Dep",
                    Guid = "new.dep",
                    ModId = 1,
                    Slug = "new-dep",
                    RecommendedVersion = "1.0.0",
                    InstallState = DependencyInstallState.NotInstalled,
                },
            },
            Removed = new List<DependencyChange>
            {
                new DependencyChange
                {
                    Name = "Old Dep",
                    Guid = "old.dep",
                    ModId = 2,
                    Slug = "old-dep",
                    RecommendedVersion = "1.0.0",
                    InstallState = DependencyInstallState.NotInstalled,
                },
            },
        };
        mod = mod.WithUpdateDependencyChanges(depDelta);

        renderer.VersionTable(new List<Mod> { mod });

        var output = console.Output;
        Assert.Contains("Blocked by dependency", output);
        Assert.Contains("ignored", output);
        Assert.Contains("Dependency changes", output);
        Assert.Contains("New Dep", output);
        Assert.Contains("Old Dep no longer required", output);
    }
}
