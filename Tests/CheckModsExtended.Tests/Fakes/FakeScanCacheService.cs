using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public class FakeScanCacheService : IScanCacheService
{
    public ScanCacheRecord? SavedRecord { get; private set; }

    public Task SaveCacheAsync(ScanCacheRecord record, CancellationToken cancellationToken = default)
    {
        SavedRecord = record;
        return Task.CompletedTask;
    }

    public Task<ScanCacheRecord?> LoadCacheAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SavedRecord);
    }
}
