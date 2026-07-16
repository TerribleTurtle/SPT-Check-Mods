using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Service for managing ignored mod updates.
/// </summary>
public interface IIgnoreService
{
    Task<bool> AddIgnoreAsync(int apiModId, string localVersion, string latestVersion, CancellationToken cancellationToken = default);
    Task<int> RemoveIgnoreAsync(int apiModId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IgnoredUpdate>> GetIgnoresAsync(CancellationToken cancellationToken = default);
}
