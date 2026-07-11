using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.UI;
using CheckMods.Tests.Fakes;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace CheckMods.Tests.Services.UI;

[Collection("ConsoleTests")]
public sealed class MisplacedModUiRendererTests
{
    [Fact]
    public void misplaced_mods_renders_list()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new MisplacedModUiRenderer(new FakeTextRenderer());

        var misplacedMod = new MisplacedMod(false, "misplaced.mod", "Misplaced Mod", "1.0.0", "wrong_folder/mod.dll");

        var report = new MisplacedModReport(
            new List<MisplacedMod> { misplacedMod },
            new List<CrossInstalledDirectory>()
        );

        renderer.MisplacedMods(report);

        var output = console.Output;
        Assert.Contains("Improperly installed mods detected", output);
        Assert.Contains("Misplaced Mod", output);
    }

    [Fact]
    public void misplaced_mods_renders_cross_installed_directories()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new MisplacedModUiRenderer(new FakeTextRenderer());

        var crossInstalled = new CrossInstalledDirectory(
            "user/mods/cross_installed_mod",
            new List<MisplacedMod>(),
            new List<MisplacedMod>(),
            true
        );
        var report = new MisplacedModReport(
            new List<MisplacedMod>(),
            new List<CrossInstalledDirectory> { crossInstalled }
        );

        renderer.MisplacedMods(report);

        var output = console.Output;
        Assert.Contains("Improperly installed mods detected", output);
        Assert.Contains("cross_installed_mod", output);
    }

    [Fact]
    public void misplaced_mods_renders_server_mod_in_client_folder()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new MisplacedModUiRenderer(new FakeTextRenderer());

        var misplacedMod = new MisplacedMod(
            true,
            "misplaced.mod",
            "Misplaced Server Mod",
            "1.0.0",
            "BepInEx/plugins/mod.dll"
        );

        var report = new MisplacedModReport(
            new List<MisplacedMod> { misplacedMod },
            new List<CrossInstalledDirectory>()
        );

        renderer.MisplacedMods(report);

        var output = console.Output;
        Assert.Contains("Server mods found in the client folder", output);
        Assert.Contains("Misplaced Server Mod", output);
    }

    [Fact]
    public void misplaced_mods_renders_non_ambiguous_cross_installed()
    {
        var console = new TestConsole();
        AnsiConsole.Console = console;
        var renderer = new MisplacedModUiRenderer(new FakeTextRenderer());

        var misplacedMod = new MisplacedMod(
            false,
            "misplaced.mod",
            "Intruder Mod",
            "1.0.0",
            "user/mods/cross_installed_mod/mod.dll"
        );
        var crossInstalled = new CrossInstalledDirectory(
            "user/mods/cross_installed_mod",
            new List<MisplacedMod> { misplacedMod },
            new List<MisplacedMod>(),
            false
        );

        var report = new MisplacedModReport(
            new List<MisplacedMod>(),
            new List<CrossInstalledDirectory> { crossInstalled }
        );

        renderer.MisplacedMods(report);

        var output = console.Output;
        Assert.Contains("Mods found inside another mod's folder under", output);
        Assert.Contains("Intruder Mod", output);
    }
}
