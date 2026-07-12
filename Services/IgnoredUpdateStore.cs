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
using Microsoft.Extensions.Logging;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Options;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// File-backed <see cref="IIgnoredUpdateStore"/>. Stores the ignored-updates list as JSON under the app-data folder and
/// caches it in memory for the lifetime of the run.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class IgnoredUpdateStore(IOptions<IgnoredUpdateOptions> options, IOptions<AppPaths> appPaths, ILogger<IgnoredUpdateStore> logger, IFileSystem fileSystem)
    : IIgnoredUpdateStore, IDisposable
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
        await LoadAsync(cancellationToken);
        return _keys.Contains(MakeKey(apiModId, localVersion, latestVersion));
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

    private async Task<List<IgnoredUpdate>> ReadFromDiskAsync(CancellationToken cancellationToken)
    {
        try
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(
                ex,
                "Could not read ignored-updates file at {Path}; treating it as empty",
                _resolvedFilePath
            );
            return [];
        }
    }

    private async Task WriteToDiskAsync(List<IgnoredUpdate> entries, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(ex, "Could not write ignored-updates file at {Path}", _resolvedFilePath);
        }
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
