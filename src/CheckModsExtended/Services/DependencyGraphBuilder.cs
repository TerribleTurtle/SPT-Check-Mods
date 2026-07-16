using System;
using System.Collections.Generic;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;

namespace CheckModsExtended.Services;

/// <summary>
/// Contains pure logic for building dependency graphs and calculating dependency changes.
/// </summary>
public static class DependencyGraphBuilder
{
    /// <summary>
    /// Builds a dependency subtree from the API dependency structure.
    /// </summary>
    /// <param name="dependency">The API dependency to build the subtree for.</param>
    /// <param name="modByGuid">A dictionary of installed mods keyed by their GUID.</param>
    /// <param name="modById">A dictionary of installed mods keyed by their ModId.</param>
    /// <param name="installedGuids">A set of GUIDs for all installed mods.</param>
    /// <param name="missingDeps">A dictionary used to track and populate missing dependencies by GUID.</param>
    /// <param name="conflicts">A list used to track and populate dependency conflicts.</param>
    /// <param name="visited">A set of visited GUIDs used to prevent infinite recursion in circular dependencies.</param>
    /// <returns>A <see cref="DependencyNode"/> representing the root of the built subtree, or null if a circular dependency was detected.</returns>
    public static DependencyNode? BuildDependencySubtree(
        ModDependency dependency,
        Dictionary<string, Mod> modByGuid,
        Dictionary<int, Mod> modById,
        ISet<string> installedGuids,
        Dictionary<string, MissingDependency> missingDeps,
        List<DependencyConflict> conflicts,
        HashSet<string> visited
    )
    {
        // Prevent circular recursion
        if (!visited.Add(dependency.Guid))
        {
            return null;
        }

        var node = BuildDependencySubtreeInternal(
            dependency,
            modByGuid,
            modById,
            installedGuids,
            missingDeps,
            conflicts,
            visited
        );

        visited.Remove(dependency.Guid);
        return node;
    }

    /// <summary>
    /// Recursively builds the dependency subtree for a given node.
    /// </summary>
    private static DependencyNode BuildDependencySubtreeInternal(
        ModDependency dependency,
        Dictionary<string, Mod> modByGuid,
        Dictionary<int, Mod> modById,
        ISet<string> installedGuids,
        Dictionary<string, MissingDependency> missingDeps,
        List<DependencyConflict> conflicts,
        HashSet<string> visited
    )
    {
        if (
            dependency.Conflict
            && !conflicts.Any(c => c.ModGuid.Equals(dependency.Guid, StringComparison.OrdinalIgnoreCase))
        )
        {
            conflicts.Add(
                new DependencyConflict
                {
                    ModName = dependency.Name,
                    ModGuid = dependency.Guid,
                    Description = "Version constraint conflict detected",
                    DependencyInfo = dependency,
                }
            );
        }

        // Try to find the installed mod for this dependency
        Mod? installedMod = null;
        if (modByGuid.TryGetValue(dependency.Guid, out var foundByGuid))
        {
            installedMod = foundByGuid;
        }
        else if (modById.TryGetValue(dependency.Id, out var foundById))
        {
            installedMod = foundById;
        }

        var isInstalled = installedMod != null || installedGuids.Contains(dependency.Guid);

        // Track missing dependencies
        if (!isInstalled && !missingDeps.ContainsKey(dependency.Guid))
        {
            // Construct the Forge download URL
            string? downloadLink = null;
            if (
                dependency.Id > 0
                && !string.IsNullOrWhiteSpace(dependency.Slug)
                && !string.IsNullOrWhiteSpace(dependency.LatestCompatibleVersion?.Version)
            )
            {
                downloadLink = ForgeUrls.Download(
                    dependency.Id,
                    dependency.Slug,
                    dependency.LatestCompatibleVersion.Version
                );
            }

            missingDeps[dependency.Guid] = new MissingDependency
            {
                Name = dependency.Name,
                Guid = dependency.Guid,
                ModId = dependency.Id,
                Slug = dependency.Slug,
                RecommendedVersion = dependency.LatestCompatibleVersion?.Version ?? "unknown",
                DownloadLink = downloadLink,
            };
        }

        // Build children from nested dependencies
        if (dependency.Dependencies is not { Count: > 0 })
        {
            return CreateDependencyNode(dependency, installedMod, isInstalled, []);
        }

        var children = dependency
            .Dependencies.Select(nestedDep =>
                BuildDependencySubtree(nestedDep, modByGuid, modById, installedGuids, missingDeps, conflicts, visited)
            )
            .Where(node => node is not null)
            .Cast<DependencyNode>()
            .ToList();

        return CreateDependencyNode(dependency, installedMod, isInstalled, children);
    }

    /// <summary>
    /// Creates a DependencyNode from dependency info and optional installed mod.
    /// </summary>
    /// <param name="dependency">The dependency info from the API.</param>
    /// <param name="installedMod">The optionally found local installed mod matching this dependency.</param>
    /// <param name="isInstalled">A flag indicating whether the dependency is already installed.</param>
    /// <param name="children">The child dependency nodes.</param>
    /// <returns>A new <see cref="DependencyNode"/>.</returns>
    public static DependencyNode CreateDependencyNode(
        ModDependency dependency,
        Mod? installedMod,
        bool isInstalled,
        List<DependencyNode> children
    )
    {
        var mod =
            installedMod
            ?? new Mod
            {
                Local = new LocalModIdentity
                {
                    Guid = dependency.Guid,
                    FilePath = string.Empty,
                    IsServerMod = true,
                    LocalName = dependency.Name,
                    LocalAuthor = string.Empty,
                    LocalVersion = dependency.LatestCompatibleVersion?.Version ?? "unknown",
                },
            };

        return new DependencyNode
        {
            Mod = mod,
            DependencyInfo = dependency,
            IsInstalled = isInstalled,
            Children = children,
        };
    }

    /// <summary>
    /// Diffs the installed and proposed dependency trees (recursively flattened by GUID) and builds the resulting set
    /// of added and removed dependencies, each annotated with its install state relative to what's installed.
    /// </summary>
    /// <param name="installedDeps">The currently installed dependencies.</param>
    /// <param name="targetDeps">The target dependencies required by the update.</param>
    /// <param name="modByGuid">A dictionary of installed mods keyed by their GUID.</param>
    /// <param name="modById">A dictionary of installed mods keyed by their ModId.</param>
    /// <param name="installedGuids">A set of GUIDs for all installed mods.</param>
    /// <returns>An <see cref="UpdateDependencyDelta"/> detailing the added and removed dependencies.</returns>
    public static UpdateDependencyDelta BuildUpdateDependencyDelta(
        List<ModDependency> installedDeps,
        List<ModDependency> targetDeps,
        Dictionary<string, Mod> modByGuid,
        Dictionary<int, Mod> modById,
        ISet<string> installedGuids
    )
    {
        var installedFlat = FlattenDependencies(installedDeps);
        var targetFlat = FlattenDependencies(targetDeps);

        var added = targetFlat
            .Where(kvp => !installedFlat.ContainsKey(kvp.Key))
            .Select(kvp => BuildDependencyChange(kvp.Value, modByGuid, modById, installedGuids))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var removed = installedFlat
            .Where(kvp => !targetFlat.ContainsKey(kvp.Key))
            .Select(kvp => BuildDependencyChange(kvp.Value, modByGuid, modById, installedGuids))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UpdateDependencyDelta(added, removed);
    }

    /// <summary>
    /// Recursively flattens a dependency tree into a GUID-keyed map. Blank GUIDs are skipped and each GUID is visited
    /// once.
    /// </summary>
    /// <param name="deps">The list of dependencies to flatten.</param>
    /// <returns>A dictionary of dependencies keyed by GUID.</returns>
    public static Dictionary<string, ModDependency> FlattenDependencies(List<ModDependency> deps)
    {
        var map = new Dictionary<string, ModDependency>(StringComparer.OrdinalIgnoreCase);
        CollectDependencies(deps, map);
        return map;
    }

    /// <summary>
    /// Recursively collects dependencies into a map. Implementation detail: skips dependencies with a blank GUID,
    /// and avoids duplicates by using TryAdd (so the first encountered dependency for a GUID is kept).
    /// </summary>
    /// <param name="deps">The list of dependencies to collect.</param>
    /// <param name="map">The dictionary to populate with flattened dependencies.</param>
    public static void CollectDependencies(List<ModDependency> deps, Dictionary<string, ModDependency> map)
    {
        foreach (var dep in deps)
        {
            if (string.IsNullOrWhiteSpace(dep.Guid) || !map.TryAdd(dep.Guid, dep))
            {
                continue;
            }

            if (dep.Dependencies is { Count: > 0 })
            {
                CollectDependencies(dep.Dependencies, map);
            }
        }
    }

    /// <summary>
    /// Builds a <see cref="DependencyChange"/> for a dependency, resolving whether it is installed and, if so, whether
    /// its installed version looks older than the latest Forge-compatible version.
    /// </summary>
    /// <param name="dependency">The dependency to build the change record for.</param>
    /// <param name="modByGuid">A dictionary of installed mods keyed by their GUID.</param>
    /// <param name="modById">A dictionary of installed mods keyed by their ModId.</param>
    /// <param name="installedGuids">A set of GUIDs for all installed mods.</param>
    /// <returns>A <see cref="DependencyChange"/> object representing the proposed change.</returns>
    public static DependencyChange BuildDependencyChange(
        ModDependency dependency,
        Dictionary<string, Mod> modByGuid,
        Dictionary<int, Mod> modById,
        ISet<string> installedGuids
    )
    {
        Mod? installedMod = null;
        if (!string.IsNullOrWhiteSpace(dependency.Guid) && modByGuid.TryGetValue(dependency.Guid, out var foundByGuid))
        {
            installedMod = foundByGuid;
        }
        else if (modById.TryGetValue(dependency.Id, out var foundById))
        {
            installedMod = foundById;
        }

        var isInstalled =
            installedMod != null
            || (!string.IsNullOrWhiteSpace(dependency.Guid) && installedGuids.Contains(dependency.Guid));

        var recommendedVersion = dependency.LatestCompatibleVersion?.Version;

        // Construct the Forge download URL when there's enough information.
        string? downloadLink = null;
        if (
            dependency.Id > 0
            && !string.IsNullOrWhiteSpace(dependency.Slug)
            && !string.IsNullOrWhiteSpace(recommendedVersion)
        )
        {
            downloadLink = ForgeUrls.Download(dependency.Id, dependency.Slug, recommendedVersion);
        }

        DependencyInstallState state;
        if (!isInstalled)
        {
            state = DependencyInstallState.NotInstalled;
        }
        else if (
            installedMod != null
            && !string.IsNullOrWhiteSpace(recommendedVersion)
            // Fallback to 0.0.0 if SemVer parsing fails so that unparseable versions are treated as extremely old
            // instead of throwing exceptions.
            && IsVersionOlder(installedMod.Local.LocalVersion, recommendedVersion)
        )
        {
            state = DependencyInstallState.InstalledOutdated;
        }
        else
        {
            state = DependencyInstallState.InstalledOk;
        }

        return new DependencyChange
        {
            Name = dependency.Name,
            Guid = dependency.Guid,
            ModId = dependency.Id,
            Slug = dependency.Slug,
            RecommendedVersion = recommendedVersion ?? "unknown",
            DownloadLink = downloadLink,
            InstallState = state,
            InstalledVersion = installedMod?.Local.LocalVersion,
            Conflict = dependency.Conflict,
        };
    }

    /// <summary>
    /// Compares two version strings to determine if the current version is older than the recommended version.
    /// Implementation detail: Uses ParseOrDefault() which falls back to "0.0.0" if SemVer parsing fails, ensuring that
    /// unparseable versions are treated as extremely old rather than causing crashes.
    /// </summary>
    /// <param name="currentVersion">The currently installed version string.</param>
    /// <param name="recommendedVersion">The recommended version string to compare against.</param>
    /// <returns>True if the current version is older than the recommended version; otherwise, false.</returns>
    private static bool IsVersionOlder(string? currentVersion, string? recommendedVersion)
    {
        return currentVersion.ParseOrDefault() < recommendedVersion.ParseOrDefault();
    }
}
