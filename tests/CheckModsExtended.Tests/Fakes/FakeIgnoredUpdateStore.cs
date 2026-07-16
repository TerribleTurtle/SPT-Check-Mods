using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IIgnoredUpdateStore"/>.
/// </summary>
public sealed class FakeIgnoredUpdateStore : IIgnoredUpdateStore
{
    private readonly List<IgnoredUpdate> _store = [];

    /// <summary>
    /// Helper to arrange the store in tests.
    /// </summary>
    public List<IgnoredUpdate> Store
    {
        get { return _store.ToList(); }
        set
        {
            _store.Clear();
            _store.AddRange(value);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IgnoredUpdate>> LoadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<IgnoredUpdate>>(_store.ToList());
    }

    /// <inheritdoc />
    public Task<bool> IsIgnoredAsync(Mod mod, CancellationToken cancellationToken = default)
    {
        if (mod.Api.ApiModId <= 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(
            _store.Any(x =>
                x.ApiModId == mod.Api.ApiModId
                && StringComparer.OrdinalIgnoreCase.Equals(x.LocalVersion, mod.Local.LocalVersion)
                && StringComparer.OrdinalIgnoreCase.Equals(x.IgnoredLatestVersion, mod.Update.LatestVersion)
            )
        );
    }

    /// <inheritdoc />
    public Task<IgnoredUpdate?> GetIgnoredUpdateAsync(Mod mod, CancellationToken cancellationToken = default)
    {
        if (mod.Api.ApiModId <= 0)
        {
            return Task.FromResult<IgnoredUpdate?>(null);
        }

        return Task.FromResult(
            _store.FirstOrDefault(x =>
                x.ApiModId == mod.Api.ApiModId
                && StringComparer.OrdinalIgnoreCase.Equals(x.LocalVersion, mod.Local.LocalVersion)
                && StringComparer.OrdinalIgnoreCase.Equals(x.IgnoredLatestVersion, mod.Update.LatestVersion)
            )
        );
    }

    /// <inheritdoc />
    public Task SaveAsync(IReadOnlyList<IgnoredUpdate> entries, CancellationToken cancellationToken = default)
    {
        _store.Clear();
        _store.AddRange(entries);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> SyncRemoteIgnoresAsync(IReadOnlyList<IgnoredUpdate> incoming, CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<int> MergeWithoutOverwriteAsync(
        IReadOnlyList<IgnoredUpdate> incoming,
        CancellationToken cancellationToken = default
    )
    {
        int added = 0;
        foreach (var entry in incoming)
        {
            if (
                !_store.Any(x =>
                    x.ApiModId == entry.ApiModId
                    && StringComparer.OrdinalIgnoreCase.Equals(x.LocalVersion, entry.LocalVersion)
                    && StringComparer.OrdinalIgnoreCase.Equals(x.IgnoredLatestVersion, entry.IgnoredLatestVersion)
                )
            )
            {
                _store.Add(entry);
                added++;
            }
        }
        return Task.FromResult(added);
    }
}
