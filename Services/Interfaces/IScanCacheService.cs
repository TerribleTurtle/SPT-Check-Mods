using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

public interface IScanCacheService
{
    Task SaveCacheAsync(ScanCacheRecord record, CancellationToken cancellationToken = default);
    Task<ScanCacheRecord?> LoadCacheAsync(CancellationToken cancellationToken = default);
}
