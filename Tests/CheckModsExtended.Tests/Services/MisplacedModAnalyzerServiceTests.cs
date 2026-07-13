using CheckModsExtended.Models;

namespace CheckModsExtended.Tests.Services;
using CheckModsExtended.Services;

/// <summary>
/// Tests for <see cref="MisplacedModReport"/>'s exclusion members, which decide what gets dropped from the remaining
/// checks once misplaced mods are detected. Pure logic with no console or I/O dependency.
/// </summary>
public sealed class MisplacedModAnalyzerServiceTests
{
    private static string MockPath(params string[] parts)
    {
        return System.IO.Path.Combine(parts);
    }

    private static MisplacedMod Mod(string name, string filePath, bool isServerMod = false)
    {
        return new MisplacedMod(isServerMod, $"com.author.{name}", name, "1.0.0", filePath);
    }

    [Fact]
    public void Empty_report_excludes_nothing()
    {
        var report = new MisplacedModReport([], []);

        Assert.False(report.Any);
        var analyzer = new MisplacedModAnalyzerService();
        Assert.Empty(analyzer.GetExcludedFilePaths(report));
        Assert.Empty(analyzer.GetExcludedDirectories(report));
    }

    [Fact]
    public void Wrong_folder_mods_are_excluded_by_file_path()
    {
        var serverInClient = Mod(
            "ServerMod",
            MockPath("SPT", "BepInEx", "plugins", "ServerMod.dll"),
            isServerMod: true
        );
        var clientInServer = Mod("ClientMod", MockPath("SPT", "SPT", "user", "mods", "ClientMod", "ClientMod.dll"));

        var report = new MisplacedModReport([serverInClient, clientInServer], []);

        Assert.True(report.Any);
        var analyzer = new MisplacedModAnalyzerService();
        Assert.Equal(new[] { serverInClient.FilePath, clientInServer.FilePath }, analyzer.GetExcludedFilePaths(report));
        Assert.Empty(analyzer.GetExcludedDirectories(report));
    }

    [Fact]
    public void Identified_cross_installed_intruder_is_excluded_but_the_legitimate_occupant_is_not()
    {
        var legitimate = Mod("HostMod", MockPath("SPT", "BepInEx", "plugins", "HostMod", "HostMod.dll"));
        var intruder = Mod("Intruder", MockPath("SPT", "BepInEx", "plugins", "HostMod", "Intruder.dll"));

        var directory = new CrossInstalledDirectory(
            MockPath("SPT", "BepInEx", "plugins", "HostMod"),
            Misplaced: [intruder],
            Mods: [legitimate, intruder],
            Ambiguous: false
        );

        var report = new MisplacedModReport([], [directory]);

        Assert.True(report.Any);
        var analyzer = new MisplacedModAnalyzerService();
        Assert.Contains(intruder.FilePath, analyzer.GetExcludedFilePaths(report));
        Assert.DoesNotContain(legitimate.FilePath, analyzer.GetExcludedFilePaths(report));
        Assert.Empty(analyzer.GetExcludedDirectories(report));
    }

    [Fact]
    public void Ambiguous_cross_installed_directory_is_excluded_by_folder_not_by_file()
    {
        var modA = Mod("ModA", MockPath("SPT", "BepInEx", "plugins", "Shared", "ModA.dll"));
        var modB = Mod("ModB", MockPath("SPT", "BepInEx", "plugins", "Shared", "ModB.dll"));

        var directory = new CrossInstalledDirectory(
            MockPath("SPT", "BepInEx", "plugins", "Shared"),
            Misplaced: [],
            Mods: [modA, modB],
            Ambiguous: true
        );

        var report = new MisplacedModReport([], [directory]);

        Assert.True(report.Any);
        var analyzer = new MisplacedModAnalyzerService();
        Assert.Equal(new[] { directory.Directory }, analyzer.GetExcludedDirectories(report));
        Assert.DoesNotContain(modA.FilePath, analyzer.GetExcludedFilePaths(report));
        Assert.DoesNotContain(modB.FilePath, analyzer.GetExcludedFilePaths(report));
    }
}
