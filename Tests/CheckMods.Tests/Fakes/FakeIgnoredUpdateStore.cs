using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

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
        get
        {
            return _store.ToList();
        }
        set
        {
            _store.Clear();
            _store.AddRange(value);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IgnoredUpdate> Load()
    {
        return _store.ToList();
    }

    /// <inheritdoc />
    public bool IsIgnored(Mod mod)
    {
        if (mod.Api.ApiModId <= 0)
        {
            return false;
        }

        return _store.Any(x => 
            x.ApiModId == mod.Api.ApiModId && 
            x.LocalVersion == mod.Local.LocalVersion && 
            x.IgnoredLatestVersion == mod.Update.LatestVersion);
    }

    /// <inheritdoc />
    public void Save(IReadOnlyList<IgnoredUpdate> entries)
    {
        _store.Clear();
        _store.AddRange(entries);
    }

    /// <inheritdoc />
    public int MergeWithoutOverwrite(IReadOnlyList<IgnoredUpdate> incoming)
    {
        int added = 0;
        foreach (var entry in incoming)
        {
            if (!_store.Any(x => x.ApiModId == entry.ApiModId && x.LocalVersion == entry.LocalVersion && x.IgnoredLatestVersion == entry.IgnoredLatestVersion))
            {
                _store.Add(entry);
                added++;
            }
        }
        return added;
    }
}






