using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Configuration;

/// <summary>
/// Configuration options for the logging infrastructure.
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>
    /// Whether file logging is enabled. Default is true.
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// The minimum log level for file logging. Default is Debug.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Maximum size of the log file in bytes before rotation. Default is 10 MB.
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Number of log files to retain. Default is 3.
    /// </summary>
    public int RetainedFileCount { get; set; } = 3;

    /// <summary>
    /// The path to the active log file. Relative paths resolve against AppDataDirectory/logs.
    /// </summary>
    public string LogFilePath { get; set; } = "checkmod.log";
}
