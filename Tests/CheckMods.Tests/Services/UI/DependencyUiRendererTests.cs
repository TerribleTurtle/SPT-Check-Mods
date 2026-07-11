using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Services.UI;
using CheckMods.Tests.Fakes;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace CheckMods.Tests.Services.UI;

[Collection("ConsoleTests")]
public sealed class DependencyUiRendererTests
{
    [Fact]
    public void dependency_results_renders_missing_deps()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new DependencyUiRenderer(new FakeTextRenderer());

        var result = new DependencyAnalysisResult
        {
            RootMods = new List<DependencyNode>
            {
                new DependencyNode
                {
                    Mod = new Mod
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
                    },
                    IsInstalled = true,
                },
            },
            Conflicts = new List<DependencyConflict>(),
            MissingDependencies = new List<MissingDependency>
            {
                new MissingDependency
                {
                    Name = "Missing Mod",
                    Guid = "missing.mod",
                    ModId = 123,
                    Slug = "missing-mod",
                    RecommendedVersion = "1.0.0",
                },
            },
        };

        renderer.DependencyResults(result);

        var output = console.Output;
        Assert.Contains("Missing Mod", output);
        Assert.Contains("1.0.0", output);
    }

    [Fact]
    public void dependency_results_renders_conflicts()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new DependencyUiRenderer(new FakeTextRenderer());

        var result = new DependencyAnalysisResult
        {
            RootMods = new List<DependencyNode>
            {
                new DependencyNode
                {
                    Mod = new Mod
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
                    },
                    IsInstalled = true,
                },
            },
            Conflicts = new List<DependencyConflict>
            {
                new DependencyConflict
                {
                    ModName = "Conflicted Mod",
                    ModGuid = "conflict.mod",
                    Description = "Version 1.0.0 conflicts with 2.0.0",
                    DependencyInfo = new ModDependency(
                        123,
                        "conflict.mod",
                        "Dependency",
                        "dependency",
                        null,
                        true,
                        null
                    ),
                },
            },
            MissingDependencies = new List<MissingDependency>(),
        };

        renderer.DependencyResults(result);

        var output = console.Output;
        Assert.Contains("Dependency conflicts", output);
        Assert.Contains("Conflicted Mod", output);
        Assert.Contains("Version 1.0.0 conflicts with 2.0.0", output);
    }

    [Fact]
    public void dependency_results_renders_empty_when_no_root_mods()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new DependencyUiRenderer(new FakeTextRenderer());

        var result = new DependencyAnalysisResult();

        renderer.DependencyResults(result);

        var output = console.Output;
        Assert.Contains("No dependency information available", output);
    }

    [Fact]
    public void dependency_results_renders_success_when_no_issues()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new DependencyUiRenderer(new FakeTextRenderer());

        var result = new DependencyAnalysisResult
        {
            RootMods = new List<DependencyNode>
            {
                new DependencyNode
                {
                    Mod = new Mod
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
                    },
                    IsInstalled = true,
                },
            },
        };

        renderer.DependencyResults(result);

        var output = console.Output;
        Assert.Contains("All dependencies are satisfied", output);
    }
}
