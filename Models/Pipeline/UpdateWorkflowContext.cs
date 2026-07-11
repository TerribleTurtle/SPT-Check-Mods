using CheckMods.Models;

namespace CheckMods.Models.Pipeline;

/// <summary>
/// Context passed through the update workflow pipeline.
/// </summary>
public sealed class UpdateWorkflowContext
{
    /// <summary>
    /// Gets or sets the command line arguments.
    /// </summary>
    public required string[] Args { get; set; }

    /// <summary>
    /// Gets or sets the path to the SPT installation.
    /// </summary>
    public string? SptPath { get; set; }

    /// <summary>
    /// Gets or sets the SPT version.
    /// </summary>
    public SemanticVersioning.Version? SptVersion { get; set; }

    /// <summary>
    /// Gets or sets the report of misplaced mods.
    /// </summary>
    public MisplacedModReport? MisplacedReport { get; set; }

    /// <summary>
    /// Gets or sets the list of mods.
    /// </summary>
    public List<Mod> Mods { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the workflow is cancelled.
    /// </summary>
    public bool IsCancelled { get; set; }
}
