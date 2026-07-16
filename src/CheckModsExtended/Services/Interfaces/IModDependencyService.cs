using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Service responsible for analyzing mod dependencies and building a dependency tree.
/// </summary>
public interface IModDependencyService
{
    /// <summary>
    /// Analyzes dependencies for a collection of mods.
    /// </summary>
    /// <remarks>
    /// The dependency analysis uses a multi-pass algorithm:
    /// 1. First pass: Identifies unique matched mods and concurrently fetches their current dependencies from the API.
    /// 2. Second pass: For mods with pending updates, concurrently fetches dependencies for their proposed versions and diffs them against current dependencies to compute deltas.
    /// 3. Third pass: Reconstructs a full dependency graph for each mod based on the computed updates and cached components.
    /// </remarks>
    /// <param name="mods">The mods to analyze dependencies for.</param>
    /// <param name="installedModGuids">Set of GUIDs for mods that are currently installed.</param>
    /// <param name="progress">Optional callback for progress updates (current, total).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A tuple containing <c>UpdatedMods</c> (the list of updated mods) and <c>Result</c> (dependency analysis result containing tree structure and any issues).</returns>
    Task<(IReadOnlyList<Mod> UpdatedMods, DependencyAnalysisResult Result)> AnalyzeDependenciesAsync(
        IEnumerable<Mod> mods,
        ISet<string> installedModGuids,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Result of dependency analysis containing the tree structure and any detected issues.
/// </summary>
public sealed class DependencyAnalysisResult
{
    /// <summary>
    /// Root mods that are not dependencies of any other installed mod.
    /// </summary>
    public List<DependencyNode> RootMods { get; init; } = [];

    /// <summary>
    /// Dependencies that have version conflicts.
    /// </summary>
    public List<DependencyConflict> Conflicts { get; init; } = [];

    /// <summary>
    /// Dependencies that are required but not installed.
    /// </summary>
    public List<MissingDependency> MissingDependencies { get; init; } = [];

    /// <summary>
    /// Whether the analysis has any issues (conflicts or missing dependencies).
    /// </summary>
    public bool HasIssues
    {
        get { return Conflicts.Count > 0 || MissingDependencies.Count > 0; }
    }
}

/// <summary>
/// Represents a node in the dependency tree.
/// </summary>
public sealed class DependencyNode
{
    /// <summary>
    /// The mod at this node.
    /// </summary>
    public required Mod Mod { get; init; }

    /// <summary>
    /// The dependency information from the API (null for root mods).
    /// </summary>
    public ModDependency? DependencyInfo { get; init; }

    /// <summary>
    /// Child dependencies of this mod.
    /// </summary>
    public List<DependencyNode> Children { get; init; } = [];

    /// <summary>
    /// Whether this dependency is installed locally.
    /// </summary>
    public bool IsInstalled { get; init; } = true;
}

/// <summary>
/// Represents a version conflict between dependencies.
/// </summary>
public sealed class DependencyConflict
{
    /// <summary>
    /// The mod that has the conflict.
    /// </summary>
    public required string ModName { get; init; }

    /// <summary>
    /// The GUID of the conflicting mod.
    /// </summary>
    public required string ModGuid { get; init; }

    /// <summary>
    /// Description of the conflict.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The dependency information with conflict details.
    /// </summary>
    public required ModDependency DependencyInfo { get; init; }
}

/// <summary>
/// Represents a missing dependency.
/// </summary>
public sealed class MissingDependency
{
    /// <summary>
    /// The name of the missing dependency.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The GUID of the missing dependency.
    /// </summary>
    public required string Guid { get; init; }

    /// <summary>
    /// The mod ID on Forge.
    /// </summary>
    public required int ModId { get; init; }

    /// <summary>
    /// The URL slug on Forge.
    /// </summary>
    public required string Slug { get; init; }

    /// <summary>
    /// The recommended version to install.
    /// </summary>
    public required string RecommendedVersion { get; init; }

    /// <summary>
    /// Download link for the dependency.
    /// </summary>
    public string? DownloadLink { get; init; }
}
