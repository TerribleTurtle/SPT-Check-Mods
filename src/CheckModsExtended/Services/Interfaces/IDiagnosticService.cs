using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Service for creating diagnostic reports.
/// </summary>
public interface IDiagnosticService
{
    /// <summary>
    /// Exports the application logs to a zip archive.
    /// </summary>
    /// <returns>The path to the zip archive, or null if no logs exist.</returns>
    Task<string?> ExportLogsAsync(CancellationToken cancellationToken = default);
}
