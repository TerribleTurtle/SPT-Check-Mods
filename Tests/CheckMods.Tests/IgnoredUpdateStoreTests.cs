using CheckMods.Tests.Fixtures;
using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CheckMods.Tests;

/// <summary>
/// Tests for <see cref="IgnoredUpdateStore"/>: file round-tripping, the match predicate (including that a genuinely
/// newer Forge version is NOT suppressed), corruption handling, and the non-overwriting merge.
/// </summary>
public sealed class IgnoredUpdateStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public IgnoredUpdateStoreTests()
    {
        _dir = TempWorkspace.CreateDirectory("checkmods-tests");
        _path = Path.Combine(_dir, "ignored-updates.json");
    }

    public void Dispose()
    {
        TempWorkspace.SafeDelete(_dir);
    }

    private IgnoredUpdateStore CreateStore()
    {
        return new IgnoredUpdateStore(
            Options.Create(new IgnoredUpdateOptions { FilePath = _path, RemoteUrl = null }),
            NullLogger<IgnoredUpdateStore>.Instance
        );
    }

    private static IgnoredUpdate Entry(int id, string local, string latest, IgnoreSource source = IgnoreSource.User)
    {
        return new IgnoredUpdate(id, local, latest, Name: $"Mod {id}", Source: source);
    }

    [Fact]
    public async Task load_returns_empty_when_file_missing()
    {
        Assert.Empty(await CreateStore().LoadAsync());
    }

    [Fact]
    public async Task save_then_load_round_trips_entries()
    {
        await CreateStore().SaveAsync([Entry(1, "1.0.0", "1.0.1"), Entry(2, "2.0.0", "2.1.0")]);

        // New store instance reads from disk.
        var reloaded = await CreateStore().LoadAsync();

        Assert.Equal(2, reloaded.Count);
        Assert.Contains(reloaded, e => e.ApiModId == 1 && e.IgnoredLatestVersion == "1.0.1");
    }

    [Fact]
    public async Task isignored_matches_on_id_and_both_versions()
    {
        var store = CreateStore();
        await store.SaveAsync([Entry(1, "1.0.0", "1.0.1")]);

        Assert.True(await store.IsIgnoredAsync(1, "1.0.0", "1.0.1"));
        // A genuinely newer Forge release (different latest) must NOT be suppressed.
        Assert.False(await store.IsIgnoredAsync(1, "1.0.0", "1.0.2"));
        // A different mod id is unrelated.
        Assert.False(await store.IsIgnoredAsync(99, "1.0.0", "1.0.1"));
    }

    [Fact]
    public async Task isignored_is_case_insensitive_on_versions()
    {
        var store = CreateStore();
        await store.SaveAsync([Entry(1, "1.0.0-BETA", "1.0.1-RC")]);

        Assert.True(await store.IsIgnoredAsync(1, "1.0.0-beta", "1.0.1-rc"));
    }

    [Fact]
    public async Task load_returns_empty_on_corrupt_file()
    {
        await File.WriteAllTextAsync(_path, "{ this is not valid json ");

        Assert.Empty(await CreateStore().LoadAsync());
    }

    [Fact]
    public async Task mergewithoutoverwrite_adds_new_and_preserves_existing()
    {
        await CreateStore().SaveAsync([Entry(1, "1.0.0", "1.0.1", IgnoreSource.User)]);

        var added = await CreateStore()
            .MergeWithoutOverwriteAsync([
                Entry(1, "1.0.0", "1.0.1", IgnoreSource.User), // duplicate key -> skipped
                Entry(2, "2.0.0", "2.1.0", IgnoreSource.User), // new -> added as Remote
            ]);

        Assert.Equal(1, added);

        var all = await CreateStore().LoadAsync();
        Assert.Equal(2, all.Count);
        Assert.Equal(IgnoreSource.User, all.Single(e => e.ApiModId == 1).Source); // existing not overwritten
        Assert.Equal(IgnoreSource.Remote, all.Single(e => e.ApiModId == 2).Source); // addition tagged remote
    }

    [Fact]
    public async Task save_does_not_leave_temp_file()
    {
        await CreateStore().SaveAsync([Entry(1, "1.0.0", "1.0.1")]);

        Assert.False(File.Exists(_path + ".tmp"));
    }
}
