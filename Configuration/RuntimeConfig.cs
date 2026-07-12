namespace CheckModsExtended.Configuration;

/// <summary>
/// A global singleton to store parsed command-line flags before execution.
/// </summary>
public sealed class RuntimeConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether the application should bypass interactive prompts.
    /// </summary>
    public bool IsHeadless { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether verbose logging is enabled.
    /// </summary>
    public bool IsVerbose { get; set; }

    /// <summary>
    /// Gets or sets the requested output format.
    /// </summary>
    public string Format { get; set; } = "table";
}
