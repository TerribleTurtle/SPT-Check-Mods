using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Interfaces;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public class DependencyGraphBuilderTests
{
    [Fact]
    public void BuildDependencySubtree_DetectsCircularDependencies_ReturnsNull()
    {
        // Arrange
        var missingDeps = new Dictionary<string, MissingDependency>();
        var conflicts = new List<DependencyConflict>();
        var visited = new HashSet<string> { "circular.guid" };
        var dep = new ModDependency(
            1, "circular.guid", "Circ", "circ",
            new DependencyVersionInfo(1, "1.0", null, null, null), false, null
        );

        // Act
        var result = DependencyGraphBuilder.BuildDependencySubtree(
            dep, new Dictionary<string, Mod>(), new Dictionary<int, Mod>(), new HashSet<string>(),
            missingDeps, conflicts, visited
        );

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void BuildDependencySubtree_CollectsMissingDependencies()
    {
        // Arrange
        var missingDeps = new Dictionary<string, MissingDependency>();
        var conflicts = new List<DependencyConflict>();
        var visited = new HashSet<string>();
        var dep = new ModDependency(
            1, "test.guid", "TestDep", "test-dep",
            new DependencyVersionInfo(1, "1.0", null, null, null), false, null
        );

        // Act
        var result = DependencyGraphBuilder.BuildDependencySubtree(
            dep, new Dictionary<string, Mod>(), new Dictionary<int, Mod>(), new HashSet<string>(),
            missingDeps, conflicts, visited
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.IsInstalled);
        Assert.True(missingDeps.ContainsKey("test.guid"));
        var missing = missingDeps["test.guid"];
        Assert.Equal("TestDep", missing.Name);
    }

    [Fact]
    public void BuildUpdateDependencyDelta_ProperlyIdentifiesAddedAndRemoved()
    {
        // Arrange
        var installedDeps = new List<ModDependency>
        {
            new ModDependency(1, "dep.1", "Dep 1", "dep-1", null, false, null)
        };
        
        var targetDeps = new List<ModDependency>
        {
            new ModDependency(2, "dep.2", "Dep 2", "dep-2", null, false, null)
        };

        var modByGuid = new Dictionary<string, Mod>
        {
            ["dep.1"] = new Mod { Local = new LocalModIdentity { Guid = "dep.1", FilePath = "", LocalName = "", LocalAuthor = "", IsServerMod = false, LocalVersion = "1.0" } }
        };
        var modById = new Dictionary<int, Mod>();
        var installedGuids = new HashSet<string> { "dep.1" };

        // Act
        var result = DependencyGraphBuilder.BuildUpdateDependencyDelta(
            installedDeps, targetDeps, modByGuid, modById, installedGuids
        );

        // Assert
        Assert.Single(result.Added);
        Assert.Equal("dep.2", result.Added[0].Guid);
        Assert.Equal(DependencyInstallState.NotInstalled, result.Added[0].InstallState);

        Assert.Single(result.Removed);
        Assert.Equal("dep.1", result.Removed[0].Guid);
        Assert.Equal(DependencyInstallState.InstalledOk, result.Removed[0].InstallState);
    }
}
