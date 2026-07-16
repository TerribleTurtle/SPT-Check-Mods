using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// File-backed <see cref="IIgnoredUpdateStore"/>. Stores the ignored-updates list as JSON under the app-data folder and
/// caches it in memory for the lifetime of the run.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class IgnoredUpdateStore(
    IOptions<IgnoredUpdateOptions> options,
    IOptions<AppPaths> appPaths,
    IFileSystem fileSystem
) : IIgnoredUpdateStore, IDisposable
{
    private readonly string _resolvedFilePath = Path.IsPathRooted(options.Value.FilePath)
        ? options.Value.FilePath
        : Path.Combine(appPaths.Value.AppDataDirectory, options.Value.FilePath);
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private List<IgnoredUpdate>? _cache;
    private HashSet<string> _keys = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<IReadOnlyList<IgnoredUpdate>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null)
            {
                return _cache;
            }

            _cache = await ReadFromDiskAsync(cancellationToken);
            RebuildKeys();
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsIgnoredAsync(Mod mod, CancellationToken cancellationToken = default)
    {
        if (!mod.Api.ApiModId.HasValue || mod.Update.LatestVersion is null)
        {
            return false;
        }

        return await IsIgnoredAsync(
            mod.Api.ApiModId.Value,
            mod.Local.LocalVersion,
            mod.Update.LatestVersion,
            cancellationToken
        );
    }

    /// <summary>Value-based overload of <see cref="IsIgnoredAsync"/>.</summary>
    internal async Task<bool> IsIgnoredAsync(
        int apiModId,
        string localVersion,
        string latestVersion,
        CancellationToken cancellationToken = default
    )
    {
        return await GetIgnoredUpdateAsync(apiModId, localVersion, latestVersion, cancellationToken) is not null;
    }

    /// <inheritdoc />
    public async Task<IgnoredUpdate?> GetIgnoredUpdateAsync(Mod mod, CancellationToken cancellationToken = default)
    {
        if (!mod.Api.ApiModId.HasValue || mod.Update.LatestVersion is null)
        {
            return null;
        }

        return await GetIgnoredUpdateAsync(
            mod.Api.ApiModId.Value,
            mod.Local.LocalVersion,
            mod.Update.LatestVersion,
            cancellationToken
        );
    }

    /// <summary>Value-based overload of <see cref="GetIgnoredUpdateAsync"/>.</summary>
    internal async Task<IgnoredUpdate?> GetIgnoredUpdateAsync(
        int apiModId,
        string localVersion,
        string latestVersion,
        CancellationToken cancellationToken = default
    )
    {
        await LoadAsync(cancellationToken);
        
        var key = MakeKey(apiModId, localVersion, latestVersion);
        var exactMatch = _cache?.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
        if (exactMatch is not null)
        {
            return exactMatch;
        }

        return _cache?.FirstOrDefault(x => 
            x.ApiModId == apiModId &&
            SemVer.AreVersionsEquivalent(x.LocalVersion, localVersion) &&
            SemVer.AreVersionsEquivalent(x.IgnoredLatestVersion, latestVersion)
        );
    }

    /// <inheritdoc />
    public async Task SaveAsync(IReadOnlyList<IgnoredUpdate> entries, CancellationToken cancellationToken = default)
    {
        var list = entries.ToList();

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await WriteToDiskAsync(list, cancellationToken);
            _cache = list;
            RebuildKeys();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> MergeWithoutOverwriteAsync(
        IReadOnlyList<IgnoredUpdate> incoming,
        CancellationToken cancellationToken = default
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is null)
            {
                _cache = await ReadFromDiskAsync(cancellationToken);
                RebuildKeys();
            }

            var current = _cache.ToList();
            var keys = current.Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var entry in incoming)
            {
                // Skip entries already present by key.
                if (!keys.Add(entry.Key))
                {
                    continue;
                }

                current.Add(
                    entry with
                    {
                        Source = IgnoreSource.Remote,
                        DismissedUtc = entry.DismissedUtc ?? DateTimeOffset.UtcNow,
                    }
                );
                added++;
            }

            if (added > 0)
            {
                await WriteToDiskAsync(current, cancellationToken);
                _cache = current;
                RebuildKeys();
            }

            return added;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<int> SyncRemoteIgnoresAsync(
        IReadOnlyList<IgnoredUpdate> remoteList,
        CancellationToken cancellationToken = default
    )
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache is null)
            {
                _cache = await ReadFromDiskAsync(cancellationToken);
            }

            var current = _cache.Where(e => e.Source == IgnoreSource.User).ToList();
            var userKeys = current.Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var entry in remoteList)
            {
                if (userKeys.Contains(entry.Key))
                {
                    continue;
                }

                current.Add(
                    entry with
                    {
                        Source = IgnoreSource.Remote,
                        DismissedUtc = entry.DismissedUtc ?? DateTimeOffset.UtcNow,
                    }
                );
                added++;
            }

            await WriteToDiskAsync(current, cancellationToken);
            _cache = current;
            RebuildKeys();

            return added;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<IgnoredUpdate>> ReadFromDiskAsync(CancellationToken cancellationToken)
    {
        if (!_fileSystem.FileExists(_resolvedFilePath))
        {
            return [];
        }

        var json = await _fileSystem.ReadAllTextAsync(_resolvedFilePath, cancellationToken);
        var file = JsonSerializer.Deserialize(
            json,
            CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.IgnoredUpdatesFile
        );
        if (file?.Ignored is null)
        {
            return [];
        }

        // Keep only well-formed entries.
        return file.Ignored.Where(e => e.IsWellFormed).ToList();
    }

    private async Task WriteToDiskAsync(List<IgnoredUpdate> entries, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_resolvedFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            _fileSystem.CreateDirectory(directory);
        }

        var file = new IgnoredUpdatesFile(IgnoredUpdatesFile.CurrentSchemaVersion, entries);
        var json = JsonSerializer.Serialize(
            file,
            CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.IgnoredUpdatesFile
        );

        // Atomic write: stage to a temp file then move into place.
        var tempPath = _resolvedFilePath + ".tmp";
        await _fileSystem.WriteAllTextAsync(tempPath, json, cancellationToken);
        _fileSystem.MoveFile(tempPath, _resolvedFilePath, true);
    }

    private void RebuildKeys()
    {
        _keys = (_cache ?? []).Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string MakeKey(int apiModId, string localVersion, string latestVersion)
    {
        return $"{apiModId}|{localVersion}|{latestVersion}";
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
