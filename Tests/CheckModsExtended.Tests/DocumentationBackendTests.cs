using System;
using System.IO;
using Xunit;

namespace CheckModsExtended.Tests;

public class DocumentationBackendTests
{
    [Fact]
    public void BackendFiles_ShouldContainExpectedDocumentation()
    {
        var baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        
        var pluginMetadataExtractorPath = Path.Combine(baseDir, "Services", "PluginMetadataExtractor.cs");
        var pluginMetadataContent = File.ReadAllText(pluginMetadataExtractorPath);
        Assert.Contains("/// <inheritdoc />\n    public async Task<(List<Mod> Mods, List<PluginDll> Plugins)> ConsolidateDirectoryModsAsync", pluginMetadataContent.Replace("\r\n", "\n"));

        var misplacedModDetectorPath = Path.Combine(baseDir, "Services", "MisplacedModDetector.cs");
        var misplacedModDetectorContent = File.ReadAllText(misplacedModDetectorPath);
        Assert.Contains("/// <summary>\n    /// Detects directories inside BepInEx/plugins that contain multiple unrelated mods installed together.\n    /// </summary>\n    private async Task<List<CrossInstalledDirectory>> DetectCrossInstalledDirectoriesAsync", misplacedModDetectorContent.Replace("\r\n", "\n"));
        Assert.Contains("/// <summary>\n    /// Determines which mod is legitimately installed in the directory and which ones are cross-installed.\n    /// </summary>\n    private CrossInstalledDirectory AttributeCrossInstall", misplacedModDetectorContent.Replace("\r\n", "\n"));

        var dependencyGraphBuilderPath = Path.Combine(baseDir, "Services", "DependencyGraphBuilder.cs");
        var dependencyGraphBuilderContent = File.ReadAllText(dependencyGraphBuilderPath);
        Assert.Contains("/// <summary>\n    /// Recursively builds the dependency subtree for a given node.\n    /// </summary>\n    private static DependencyNode BuildDependencySubtreeInternal", dependencyGraphBuilderContent.Replace("\r\n", "\n"));

        var modEnrichmentServicePath = Path.Combine(baseDir, "Services", "ModEnrichmentService.cs");
        var modEnrichmentServiceContent = File.ReadAllText(modEnrichmentServicePath);
        Assert.Contains("        /// <summary>\n        /// Processes updates for a generic collection of items in batches to avoid rate limits.\n        /// </summary>\n        void ProcessUpdates<T>", modEnrichmentServiceContent.Replace("\r\n", "\n"));
    }
}
