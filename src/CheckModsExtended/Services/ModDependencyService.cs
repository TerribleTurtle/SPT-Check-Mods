using System.Collections.Concurrent;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services;

/// <summary>
/// Service responsible for analyzing mod dependencies and building a dependency tree.
/// </summary>
public sealed class ModDependencyService(IModUpdateClient forgeApiService, ILogger<ModDependencyService> logger)
    : IModDependencyService
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<Mod> UpdatedMods, DependencyAnalysisResult Result)> AnalyzeDependenciesAsync(
        IEnumerable<Mod> mods,
        ISet<string> installedModGuids,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Analyzing mod dependencies");

        var modList = mods.ToList();
        var result = new DependencyAnalysisResult();

        // Only analyze mods that are matched with the API
        var matchedMods = modList.Where(m => m.IsMatched && m.Api.ApiModId.HasValue).ToList();
        if (matchedMods.Count == 0)
        {
            logger.LogDebug("No matched mods to analyze for dependencies");
            // No matched mods, return all as roots with no children
            result.RootMods.AddRange(modList.Select(m => new DependencyNode { Mod = m }));
            return (modList, result);
        }
        logger.LogDebug("Analyzing dependencies for {ModCount} matched mods", matchedMods.Count);

        // Build lookup maps for finding installed mods
        var modByGuid = modList
            .Where(m => !string.IsNullOrWhiteSpace(m.Local.Guid))
            .GroupBy(m => m.Local.Guid, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var modById = matchedMods
            .Where(m => m.Api.ApiModId.HasValue)
            .GroupBy(m => m.Api.ApiModId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // Track all missing dependencies and conflicts across all mods
        var missingDeps = new Dictionary<string, MissingDependency>(StringComparer.OrdinalIgnoreCase);
        List<DependencyConflict> conflicts = [];

        // Fetch dependencies for each matched mod individually
        var modDependencyCache = new ConcurrentDictionary<int, List<ModDependency>>();

        // Get unique mods to fetch (by mod ID)
        var uniqueModsToFetch = matchedMods
            .Where(m => m.Api.ApiModId.HasValue)
            .GroupBy(m => m.Api.ApiModId!.Value)
            .Select(g => g.First())
            .ToList();

        // Mods with an available update get a second dependency fetch at the proposed version. Dedupe by API mod ID
        // (paired server/client components share an ID).
        var updatableGroups = modList
            .Where(m =>
                m.IsMatched
                && m.Api.ApiModId.HasValue
                && m.Update.UpdateStatus == UpdateStatus.UpdateAvailable
                && !string.IsNullOrWhiteSpace(m.Update.LatestVersion)
            )
            .GroupBy(m => m.Api.ApiModId!.Value)
            .ToList();

        var totalToFetch = uniqueModsToFetch.Count + updatableGroups.Count;
        var fetchedCount = 0;

        await Parallel.ForEachAsync(
            uniqueModsToFetch,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (mod, ct) =>
            {
                var modId = mod.Api.ApiModId!.Value;

                var depsResult = await forgeApiService.GetModDependenciesAsync(
                    [(modId.ToString(), mod.Local.LocalVersion)],
                    ct
                );

                // Extract dependencies or use empty list on error/not found
                var deps = depsResult.Match(
                    dependencies => dependencies,
                    _ => [], // NotFound
                    _ => [] // ApiError
                );

                modDependencyCache[modId] = deps;
                var current = Interlocked.Increment(ref fetchedCount);
                progress?.Report(current);
            }
        );

        // Store updates to apply sequentially
        var updateDeltas = new ConcurrentBag<(List<Mod> GroupMods, UpdateDependencyDelta Delta)>();

        // Second pass: for each updatable mod, fetch dependencies at the proposed version and diff them against the
        // installed version's dependencies (already cached above).
        await Parallel.ForEachAsync(
            updatableGroups,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (group, ct) =>
            {
                var modId = group.Key;
                var targetVersion = group.First().Update.LatestVersion!;

                var targetResult = await forgeApiService.GetModDependenciesAsync(
                    [(modId.ToString(), targetVersion)],
                    ct
                );

                var current = Interlocked.Increment(ref fetchedCount);
                progress?.Report(current);

                // Skip the diff on a not-found/error response; an empty success list is a valid "no dependencies".
                var targetDeps = targetResult.Match(
                    dependencies => (List<ModDependency>?)dependencies,
                    _ => null,
                    _ => null
                );
                if (targetDeps is null)
                {
                    return;
                }

                var installedDeps = modDependencyCache.GetValueOrDefault(modId, []);
                var delta = DependencyGraphBuilder.BuildUpdateDependencyDelta(
                    installedDeps,
                    targetDeps,
                    modByGuid,
                    modById,
                    installedModGuids
                );
                if (delta.HasChanges)
                {
                    updateDeltas.Add((group.ToList(), delta));
                }
            }
        );

        // Apply deltas
        var modIndexMap = new Dictionary<Mod, int>();
        for (var i = 0; i < modList.Count; i++)
        {
            modIndexMap.TryAdd(modList[i], i);
        }

        foreach (var (groupMods, delta) in updateDeltas)
        {
            foreach (var mod in groupMods)
            {
                if (modIndexMap.TryGetValue(mod, out var idx))
                {
                    modList[idx] = mod.WithUpdateDependencyChanges(delta);
                }
                else
                {
                    idx = modList.IndexOf(mod);
                    if (idx >= 0)
                    {
                        modList[idx] = mod.WithUpdateDependencyChanges(delta);
                    }
                }
            }
        }

        // Build the tree for each mod
        foreach (var mod in modList)
        {
            // Get dependencies for this mod (if it's matched)
            List<ModDependency> modDeps = [];
            if (mod.Api.ApiModId.HasValue && modDependencyCache.TryGetValue(mod.Api.ApiModId.Value, out var cachedDeps))
            {
                modDeps = cachedDeps;
            }

            // Build the dependency subtree for this mod
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { mod.Local.Guid };
            var children = modDeps
                .Select(dep =>
                    DependencyGraphBuilder.BuildDependencySubtree(
                        dep,
                        modByGuid,
                        modById,
                        installedModGuids,
                        missingDeps,
                        conflicts,
                        visited
                    )
                )
                .Where(node => node is not null)
                .Cast<DependencyNode>()
                .ToList();

            result.RootMods.Add(
                new DependencyNode
                {
                    Mod = mod,
                    DependencyInfo = null,
                    IsInstalled = true,
                    Children = children,
                }
            );
        }

        result.Conflicts.AddRange(conflicts);
        result.MissingDependencies.AddRange(missingDeps.Values);

        logger.LogDebug(
            "Dependency analysis complete. Conflicts: {ConflictCount}, Missing: {MissingCount}",
            conflicts.Count,
            missingDeps.Count
        );

        return (modList, result);
    }
}
