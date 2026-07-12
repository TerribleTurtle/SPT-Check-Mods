using CheckModsExtended.Models;

namespace CheckModsExtended.Models.Pipeline;

/// <summary>
/// Context passed through the update workflow pipeline.
/// </summary>
public sealed class UpdateWorkflowContext
{
    /// <summary>
    /// Gets or sets the command line arguments.
    /// Initialized by the entry point when the workflow context is created.
    /// </summary>
    public required string[] Args { get; set; }

    /// <summary>
    /// Gets or sets the path to the SPT installation.
    /// Populated by the location detection step early in the pipeline.
    /// </summary>
    public string? SptPath { get; set; }

    /// <summary>
    /// Gets or sets the SPT version.
    /// Populated by the version extraction step after the installation path is determined.
    /// </summary>
    public SemanticVersioning.Version? SptVersion { get; set; }

    /// <summary>
    /// Gets or sets the report of misplaced mods.
    /// Populated by the folder structure validation step.
    /// </summary>
    public MisplacedModReport? MisplacedReport { get; set; }

    /// <summary>
    /// Gets or sets the list of mods.
    /// Populated and enriched during the mod scanning and remote metadata fetching steps.
    /// </summary>
    public List<Mod> Mods { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the workflow is cancelled.
    /// Set by any step in the pipeline to gracefully halt further execution.
    /// </summary>
    public bool IsCancelled { get; set; }
}
