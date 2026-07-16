using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Services;

public class IgnoreService : IIgnoreService
{
    private readonly IIgnoredUpdateStore _store;

    public IgnoreService(IIgnoredUpdateStore store)
    {
        _store = store;
    }

    public async Task<bool> AddIgnoreAsync(int apiModId, string localVersion, string latestVersion, CancellationToken cancellationToken = default)
    {
        var ignores = (await _store.LoadAsync(cancellationToken)).ToList();
        var newIgnore = new IgnoredUpdate(
            ApiModId: apiModId,
            LocalVersion: localVersion,
            IgnoredLatestVersion: latestVersion,
            Source: IgnoreSource.User,
            DismissedUtc: DateTimeOffset.UtcNow
        );

        if (ignores.Any(i => i.ApiModId == apiModId && 
            CheckModsExtended.Utils.SemVer.AreVersionsEquivalent(i.LocalVersion, localVersion) && 
            CheckModsExtended.Utils.SemVer.AreVersionsEquivalent(i.IgnoredLatestVersion, latestVersion)))
        {
            return false;
        }

        ignores.Add(newIgnore);
        await _store.SaveAsync(ignores, cancellationToken);
        return true;
    }

    public async Task<int> RemoveIgnoreAsync(int apiModId, CancellationToken cancellationToken = default)
    {
        var ignores = (await _store.LoadAsync(cancellationToken)).ToList();
        var removedCount = ignores.RemoveAll(i => i.ApiModId == apiModId);

        if (removedCount > 0)
        {
            await _store.SaveAsync(ignores, cancellationToken);
        }

        return removedCount;
    }

    public async Task<IReadOnlyList<IgnoredUpdate>> GetIgnoresAsync(CancellationToken cancellationToken = default)
    {
        return await _store.LoadAsync(cancellationToken);
    }
}
