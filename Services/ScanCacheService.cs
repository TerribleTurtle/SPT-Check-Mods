using System;
using System.IO;
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

[Injectable(InjectionType.Singleton)]
public sealed class ScanCacheService : IScanCacheService, IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ScanCacheService> _logger;
    private readonly string _resolvedFilePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ScanCacheService(
        IFileSystem fileSystem,
        ILogger<ScanCacheService> logger,
        IOptions<AppPaths> appPaths)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _resolvedFilePath = Path.Combine(appPaths.Value.AppDataDirectory, "scan_cache.json");
    }

    public async Task SaveCacheAsync(ScanCacheRecord record, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var directory = Path.GetDirectoryName(_resolvedFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(
                record,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ScanCacheRecord
            );

            var tempPath = _resolvedFilePath + ".tmp";
            await _fileSystem.WriteAllTextAsync(tempPath, json, cancellationToken);
            _fileSystem.MoveFile(tempPath, _resolvedFilePath, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Could not write scan cache file at {Path}", _resolvedFilePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ScanCacheRecord?> LoadCacheAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_fileSystem.FileExists(_resolvedFilePath))
            {
                return null;
            }

            var json = await _fileSystem.ReadAllTextAsync(_resolvedFilePath, cancellationToken);
            return JsonSerializer.Deserialize(
                json,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ScanCacheRecord
            );
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.LogWarning(ex, "Could not read scan cache file at {Path}; treating as empty", _resolvedFilePath);
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
