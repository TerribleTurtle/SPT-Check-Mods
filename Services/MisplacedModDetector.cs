using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class MisplacedModDetector(
    IPluginScanCache pluginScanCache,
    IModPartitioner partitioner,
    IPluginMetadataExtractor pluginExtractor,
    IServerModExtractor serverExtractor,
    ILogger<MisplacedModDetector> logger,
    CheckModsExtended.Utils.IFileSystem fileSystem
) : IMisplacedModDetector
{
    /// <inheritdoc />
    public async Task<MisplacedModReport> DetectMisplacedModsAsync(
        string sptPath,
        CancellationToken cancellationToken = default
    )
    {
        List<MisplacedMod> wrongFolder = [];

        var serverModsDir = Path.Combine(sptPath, "SPT", "user", "mods");
        if (fileSystem.DirectoryExists(serverModsDir))
        {
            foreach (var dllPath in fileSystem.GetFiles(serverModsDir, "*.dll", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var clientMod = await Task.Run(
                    () => pluginExtractor.TryDetectClientModAsync(dllPath, cancellationToken),
                    cancellationToken
                );
                if (clientMod is not null)
                {
                    wrongFolder.Add(
                        new MisplacedMod(
                            false,
                            clientMod.Local.Guid,
                            clientMod.Local.LocalName,
                            clientMod.Local.LocalVersion,
                            dllPath
                        )
                    );
                }
            }
        }

        var pluginsDir = Path.Combine(sptPath, "BepInEx", "plugins");
        if (fileSystem.DirectoryExists(pluginsDir))
        {
            foreach (var dllPath in pluginExtractor.GetValidClientDllFiles(pluginsDir))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // OPTIMIZATION: Check cache first to avoid double Mono.Cecil parses
                var directory = ModPathUtils.GetModDirectory(dllPath, pluginsDir);
                if (
                    pluginScanCache.TryGetPlugins(directory, out var cachedPlugins)
                    && cachedPlugins!.Any(p => string.Equals(p.DllPath, dllPath, StringComparison.OrdinalIgnoreCase))
                )
                {
                    continue; // Already a verified client mod
                }

                var serverMod = await Task.Run(
                    () => serverExtractor.ExtractServerModMetadataAsync(dllPath, sptPath, cancellationToken),
                    cancellationToken
                );
                if (serverMod is not null)
                {
                    wrongFolder.Add(
                        new MisplacedMod(
                            true,
                            serverMod.Local.Guid,
                            serverMod.Local.LocalName,
                            serverMod.Local.LocalVersion,
                            dllPath
                        )
                    );
                }
            }
        }

        var crossInstalled = await DetectCrossInstalledDirectoriesAsync(pluginsDir, cancellationToken);

        logger.LogDebug(
            "Detected {WrongFolder} misplaced mods and {CrossInstalled} cross-installed directories",
            wrongFolder.Count,
            crossInstalled.Count
        );

        return new MisplacedModReport(wrongFolder, crossInstalled);
    }

    /// <summary>
    /// Detects directories inside BepInEx/plugins that contain multiple unrelated mods installed together.
    /// </summary>
    private async Task<List<CrossInstalledDirectory>> DetectCrossInstalledDirectoriesAsync(
        string pluginsDir,
        CancellationToken cancellationToken
    )
    {
        List<CrossInstalledDirectory> crossInstalled = [];

        if (!fileSystem.DirectoryExists(pluginsDir))
        {
            return crossInstalled;
        }

        var dllsByDirectory = ModPathUtils.GroupDllsByDirectory(
            pluginExtractor.GetValidClientDllFiles(pluginsDir),
            pluginsDir
        );
        dllsByDirectory.Remove(pluginsDir);

        foreach (var (directory, directoryDlls) in dllsByDirectory)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!pluginScanCache.TryGetPlugins(directory, out var pluginsAsReadOnly))
            {
                var p = await Task.Run(
                    () => pluginExtractor.ReadPluginDllsAsync(directoryDlls, cancellationToken),
                    cancellationToken
                );
                pluginsAsReadOnly = p;
            }

            var plugins = pluginsAsReadOnly!.ToList();

            if (plugins.Count < 2)
            {
                continue;
            }

            var components = partitioner.PartitionByRelatedness(plugins);
            if (components.Count < 2)
            {
                continue;
            }

            crossInstalled.Add(AttributeCrossInstall(directory, components));
        }

        return crossInstalled;
    }

    /// <summary>
    /// Determines which mod is legitimately installed in the directory and which ones are cross-installed.
    /// </summary>
    private CrossInstalledDirectory AttributeCrossInstall(string directory, List<List<PluginDll>> components)
    {
        var directoryName = Path.GetFileName(directory);
        var allMods = components.Select(group => pluginExtractor.ToMisplacedMod(group, directoryName)).ToList();

        int? legitimateIndex = null;

        var folderMatches = components
            .Select((group, index) => (group, index))
            .Where(item =>
                item.group.Any(plugin =>
                    MatchesFolderName(Path.GetFileNameWithoutExtension(plugin.DllPath), directoryName)
                )
            )
            .Select(item => item.index)
            .ToList();

        if (folderMatches.Count == 1)
        {
            legitimateIndex = folderMatches[0];
        }
        else
        {
            var maxSize = components.Max(group => group.Count);
            var largest = components
                .Select((group, index) => (group, index))
                .Where(item => item.group.Count == maxSize)
                .Select(item => item.index)
                .ToList();

            if (largest.Count == 1)
            {
                legitimateIndex = largest[0];
            }
        }

        if (legitimateIndex is null)
        {
            return new CrossInstalledDirectory(directory, [], allMods, Ambiguous: true);
        }

        var misplaced = allMods.Where((_, index) => index != legitimateIndex).ToList();
        return new CrossInstalledDirectory(directory, misplaced, allMods, Ambiguous: false);
    }

    private static bool MatchesFolderName(string fileName, string directoryName)
    {
        var file = NormalizeIdentifier(fileName);
        var directory = NormalizeIdentifier(directoryName);

        if (file.Length == 0 || directory.Length == 0)
        {
            return false;
        }

        return file.StartsWith(directory, StringComparison.Ordinal)
            || directory.StartsWith(file, StringComparison.Ordinal);
    }

    private static string NormalizeIdentifier(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}
