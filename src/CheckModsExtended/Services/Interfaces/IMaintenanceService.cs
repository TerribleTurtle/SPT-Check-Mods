using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Service for performing application maintenance tasks.
/// </summary>
public interface IMaintenanceService
{
    /// <summary>
    /// Cleans the application data directory.
    /// </summary>
    /// <returns>True if the directory existed and was cleaned, false if it did not exist.</returns>
    Task<bool> CleanAppDataAsync(CancellationToken cancellationToken = default);
}
