using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Utils;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Utils;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Unified service for scanning both server and client mods from disk. Returns unified Mod objects with validation warnings.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ModScannerService(
    IPluginScanCache pluginScanCache,
    IPluginMetadataExtractor pluginExtractor,
    IServerModExtractor serverExtractor,
    IMisplacedModDetector misplacedDetector,
    IModCheckReporter reporter,
    ILogger<ModScannerService> logger,
    IFileSystem fileSystem
) : IModScannerService
{

    /// <inheritdoc />
    public async Task<List<Mod>> ScanServerModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Scanning server mods at: {SptPath}", sptPath);

        var modsDir = Path.Combine(sptPath, "SPT", "user", "mods");
        var concurrentMods = new ConcurrentBag<Mod>();

        if (!fileSystem.DirectoryExists(modsDir))
        {
            logger.LogDebug("Server mods directory not found: {ModsDir}", modsDir);
            reporter.Status("Scanning server mods... none found.");
            return [.. concurrentMods];
        }

        var modDirs = fileSystem.GetDirectories(modsDir);
        logger.LogDebug("Found {DirCount} mod directories", modDirs.Length);
        reporter.Blank();
        reporter.Status($"Scanning {modDirs.Length} mod directories for server mods...");

        await Parallel.ForEachAsync(
            modDirs,
            cancellationToken,
            async (modDir, ct) =>
            {
                var dllFiles = fileSystem.GetFiles(modDir, "*.dll", SearchOption.TopDirectoryOnly);
                bool foundMod = false;

                foreach (var dllPath in dllFiles)
                {
                    try
                    {
                        var mod = await Task.Run(() => serverExtractor.ExtractServerModMetadataAsync(dllPath, sptPath, ct), ct);
                        if (mod is not null)
                        {
                            concurrentMods.Add(mod);
                            foundMod = true;
                            break; // Only one mod per directory
                        }
                    }
                    catch (Exception ex)
                        when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                    {
                        reporter.CouldNotReadModDll(Path.GetFileName(dllPath), ex.Message);
                    }
                }

                if (!foundMod)
                {
                    var packageMod = await serverExtractor.ExtractServerModPackageMetadataAsync(modDir, ct);
                    if (packageMod is not null)
                    {
                        concurrentMods.Add(packageMod);
                    }
                }
            }
        );

        var mods = concurrentMods.ToList();

        logger.LogInformation("Found {ModCount} server mods", mods.Count);
        reporter.Status($"Found {mods.Count} server mods.");
        return mods;
    }

    /// <inheritdoc />
    public async Task<List<Mod>> ScanClientModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Scanning client mods at: {SptPath}", sptPath);

        var pluginsDir = Path.Combine(sptPath, "BepInEx", "plugins");
        List<Mod> mods = [];

        if (!fileSystem.DirectoryExists(pluginsDir))
        {
            logger.LogWarning("BepInEx plugins directory not found: {PluginsDir}", pluginsDir);
            reporter.PluginsDirectoryNotFound(pluginsDir);
            return mods;
        }

        var dllFiles = pluginExtractor.GetValidClientDllFiles(pluginsDir);
        if (dllFiles.Count == 0)
        {
            reporter.Status("No DLL files found in plugins directory.");
            return mods;
        }

        reporter.Status($"Scanning {dllFiles.Count} DLL files for BepInEx plugins...");

        var dllsByDirectory = ModPathUtils.GroupDllsByDirectory(dllFiles, pluginsDir);
        var concurrentMods = new ConcurrentBag<Mod>();

        // Process loose DLLs (directly in plugins folder) as individual mods
        if (dllsByDirectory.TryGetValue(pluginsDir, out var looseDlls))
        {
            var looseResults = await Task.Run(() => pluginExtractor.ProcessClientDllsInParallelAsync(looseDlls, cancellationToken), cancellationToken);
            foreach (var mod in looseResults)
            {
                concurrentMods.Add(mod);
            }
            dllsByDirectory.Remove(pluginsDir);
        }

        reporter.Status($"Scanning {dllsByDirectory.Count} plugin directories for BepInEx plugins...");

        await Parallel.ForEachAsync(
            dllsByDirectory,
            new ParallelOptions { CancellationToken = cancellationToken },
            async (kvp, ct) =>
            {
                var directory = kvp.Key;
                var directoryDlls = kvp.Value;

                var (dirMods, allPlugins) = await Task.Run(() => pluginExtractor.ConsolidateDirectoryModsAsync(directory, directoryDlls, ct), ct);
                pluginScanCache.AddPlugins(directory, allPlugins);

                foreach (var m in dirMods)
                {
                    concurrentMods.Add(m);
                }
            }
        );

        mods.AddRange(concurrentMods);

        logger.LogInformation("Found {ModCount} client mods", mods.Count);
        reporter.Status($"Found {mods.Count} client mods.");
        return mods;
    }

    /// <summary>
    /// Groups DLL files by their immediate parent directory.
    /// </summary>
    /// <param name="dllFiles">A list of all discovered DLL files.</param>
    /// <param name="pluginsDir">The absolute path to the plugins directory.</param>

    /// <inheritdoc />
    public async Task<(List<Mod> ServerMods, List<Mod> ClientMods)> ScanAllModsAsync(
        string sptPath,
        CancellationToken cancellationToken = default
    )
    {
        var serverMods = await ScanServerModsAsync(sptPath, cancellationToken);
        var clientMods = await ScanClientModsAsync(sptPath, cancellationToken);
        return (serverMods, clientMods);
    }

    /// <inheritdoc />
    public string? GetSptVersion(string sptPath)
    {
        var coreDllPath = Path.Combine(sptPath, "SPT", "SPTarkov.Server.Core.dll");

        if (!fileSystem.FileExists(coreDllPath))
        {
            return null;
        }

        try
        {
            var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(coreDllPath);
            return versionInfo.FileVersion;
        }
        catch (Exception ex)
            when (ex
                    is IOException
                        or UnauthorizedAccessException
                        or FileNotFoundException
                        or System.ComponentModel.Win32Exception
            )
        {
            reporter.CouldNotReadSptVersion(ex.Message);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<MisplacedModReport> DetectMisplacedModsAsync(
        string sptPath,
        CancellationToken cancellationToken = default
    )
    {
        return await misplacedDetector.DetectMisplacedModsAsync(sptPath, cancellationToken);
    }
}



