using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class MisplacedModDetector(
    IModPartitioner partitioner,
    IPluginMetadataExtractor pluginExtractor,
    IServerModExtractor serverExtractor,
    ILogger<MisplacedModDetector> logger
) : IMisplacedModDetector
{
    /// <inheritdoc />
    public async Task<MisplacedModReport> DetectMisplacedModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        List<MisplacedMod> wrongFolder = [];

        var serverModsDir = Path.Combine(sptPath, "SPT", "user", "mods");
        if (Directory.Exists(serverModsDir))
        {
            foreach (var dllPath in Directory.GetFiles(serverModsDir, "*.dll", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var clientMod = await pluginExtractor.TryDetectClientModAsync(dllPath, cancellationToken);
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
        if (Directory.Exists(pluginsDir))
        {
            foreach (var dllPath in pluginExtractor.GetValidClientDllFiles(pluginsDir))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var serverMod = await serverExtractor.ExtractServerModMetadataAsync(dllPath, sptPath, cancellationToken);
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

    private async Task<List<CrossInstalledDirectory>> DetectCrossInstalledDirectoriesAsync(
        string pluginsDir,
        CancellationToken cancellationToken
    )
    {
        List<CrossInstalledDirectory> crossInstalled = [];

        if (!Directory.Exists(pluginsDir))
        {
            return crossInstalled;
        }

        var dllsByDirectory = GroupDllsByDirectory(pluginExtractor.GetValidClientDllFiles(pluginsDir), pluginsDir);
        dllsByDirectory.Remove(pluginsDir);

        foreach (var (directory, directoryDlls) in dllsByDirectory)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var plugins = await pluginExtractor.ReadPluginDllsAsync(directoryDlls, cancellationToken);
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

    private static Dictionary<string, List<string>> GroupDllsByDirectory(List<string> dllFiles, string pluginsDir)
    {
        return dllFiles
            .GroupBy(dllPath => GetModDirectory(dllPath, pluginsDir), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    private static string GetModDirectory(string dllPath, string pluginsDir)
    {
        var directory = Path.GetDirectoryName(dllPath);

        if (directory is null || directory.Equals(pluginsDir, StringComparison.OrdinalIgnoreCase))
        {
            return pluginsDir;
        }

        while (true)
        {
            var parent = Path.GetDirectoryName(directory);
            if (parent is null || parent.Equals(pluginsDir, StringComparison.OrdinalIgnoreCase))
            {
                return directory;
            }

            directory = parent;
        }
    }
}

