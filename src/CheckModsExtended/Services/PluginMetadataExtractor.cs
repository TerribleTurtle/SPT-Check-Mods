using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Utils;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class PluginMetadataExtractor(
    IModPartitioner partitioner,
    IOptions<ModScannerOptions> options,
    ILogger<PluginMetadataExtractor> logger,
    IModCheckReporter reporter,
    IFileSystem fileSystem,
    IBinaryParser binaryParser
) : IPluginMetadataExtractor
{
    private readonly ModScannerOptions _options = options.Value;
    private readonly IFileSystem _fileSystem = fileSystem;

    /// <inheritdoc />
    public List<string> GetValidClientDllFiles(string pluginsPath)
    {
        var sptDir = Path.GetFullPath(Path.Combine(pluginsPath, "spt")) + Path.DirectorySeparatorChar;

        return _fileSystem
            .GetFiles(pluginsPath, "*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.StartsWith(sptDir, StringComparison.OrdinalIgnoreCase))
            .Where(f => _fileSystem.GetFileLength(f) <= _options.MaxDllSizeBytes)
            .ToList();
    }

    /// <inheritdoc />
    public Task<List<Mod>> ProcessClientDllsInParallelAsync(
        List<string> dllFiles,
        CancellationToken cancellationToken = default
    )
    {
        var warnings = new ConcurrentBag<(string FileName, string Reason)>();
        var results = new ConcurrentBag<Mod?>();

        return Task.Run(
            () =>
            {
                Parallel.ForEach(
                    dllFiles,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = cancellationToken,
                    },
                    (dllPath, state) =>
                    {
                        var mod = ExtractClientModMetadata(dllPath, warnings, cancellationToken);
                        results.Add(mod);
                    }
                );

                foreach (var (fileName, reason) in warnings)
                {
                    logger.LogDebug(
                        "Could not extract client mod metadata from {FileName}: {Reason}",
                        fileName,
                        reason
                    );
                    reporter.CouldNotReadModDll(fileName, reason);
                }

                var mods = results.Where(r => r is not null).Cast<Mod>().ToList();
                return FilterDuplicateClientMods(mods);
            },
            cancellationToken
        );
    }

    private static List<Mod> FilterDuplicateClientMods(List<Mod> mods)
    {
        return mods.DistinctBy(m => (m.Local.LocalName.ToLowerInvariant(), m.Local.LocalAuthor.ToLowerInvariant()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<(List<Mod> Mods, List<PluginDll> Plugins)> ConsolidateDirectoryModsAsync(
        string directory,
        List<string> dllPaths,
        CancellationToken cancellationToken = default
    )
    {
        var allPlugins = await ReadPluginDllsAsync(dllPaths, cancellationToken);

        if (allPlugins.Count == 0)
        {
            return ([], []);
        }

        var directoryName = Path.GetFileName(directory);

        var mods = partitioner
            .PartitionByRelatedness(allPlugins)
            .Select(group => CreateConsolidatedMod(group, directoryName))
            .ToList();

        return (mods, allPlugins);
    }

    /// <inheritdoc />
    public Task<Mod?> TryDetectClientModAsync(string dllPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () =>
            {
                try
                {
                    var plugin = binaryParser.ExtractBepInPlugin(dllPath);
                    if (plugin is not null)
                    {
                        return CreateModFromBepInPlugin(plugin, dllPath);
                    }
                }
                catch (Exception ex)
                    when (ex
                            is IOException
                                or UnauthorizedAccessException
                                or BadImageFormatException
                                or System.Security.SecurityException
                    )
                {
                    logger.LogDebug(ex, "Could not inspect DLL as a client mod: {DllPath}", dllPath);
                    reporter.CouldNotReadModDll(Path.GetFileName(dllPath), ex.Message);
                }

                return null;
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<List<PluginDll>> ReadPluginDllsAsync(
        List<string> dllPaths,
        CancellationToken cancellationToken = default
    )
    {
        return Task.Run(
            () =>
            {
                List<PluginDll> plugins = [];

                foreach (var dllPath in dllPaths)
                {
                    try
                    {
                        var info = binaryParser.ReadPluginDllInfo(dllPath);

                        if (info.Plugin is null)
                        {
                            continue;
                        }

                        plugins.Add(new PluginDll(dllPath, info.Plugin, info.AssemblyName, info.ReferencedNames ?? []));
                    }
                    catch (Exception ex)
                        when (ex
                                is IOException
                                    or UnauthorizedAccessException
                                    or BadImageFormatException
                                    or System.Security.SecurityException
                        )
                    {
                        logger.LogDebug(ex, "Skipping unreadable plugin DLL: {DllPath}", dllPath);
                        reporter.CouldNotReadModDll(Path.GetFileName(dllPath), ex.Message);
                    }
                }

                return plugins;
            },
            cancellationToken
        );
    }

    /// <inheritdoc />
    public MisplacedMod ToMisplacedMod(List<PluginDll> group, string directoryName)
    {
        var (primaryDll, primaryPlugin) = SelectprimaryPlugin(
            group.Select(plugin => (plugin.DllPath, plugin.Plugin)).ToList(),
            directoryName
        );

        var mod = CreateModFromBepInPlugin(primaryPlugin, primaryDll);
        return new MisplacedMod(false, mod.Local.Guid, mod.Local.LocalName, mod.Local.LocalVersion, primaryDll);
    }

    private Mod? ExtractClientModMetadata(
        string dllPath,
        ConcurrentBag<(string FileName, string Reason)> warnings,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var plugin = binaryParser.ExtractBepInPlugin(dllPath);
            if (plugin is not null)
            {
                return CreateModFromBepInPlugin(plugin, dllPath);
            }

            return null;
        }
        catch (Exception ex)
            when (ex
                    is IOException
                        or UnauthorizedAccessException
                        or BadImageFormatException
                        or System.Security.SecurityException
            )
        {
            warnings.Add((Path.GetFileName(dllPath), ex.Message));
            return null;
        }
    }

    private static Mod CreateConsolidatedMod(List<PluginDll> group, string directoryName)
    {
        var plugins = group.Select(item => (item.DllPath, item.Plugin)).ToList();

        var (primaryDll, primaryPlugin) = SelectprimaryPlugin(plugins, directoryName);

        var mod = CreateModFromBepInPlugin(primaryPlugin, primaryDll);

        var alternateGuids = plugins
            .Select(item => item.Plugin.Guid)
            .Where(guid => !guid.Equals(primaryPlugin.Guid, StringComparison.OrdinalIgnoreCase))
            .Except(mod.Local.AlternateGuids, StringComparer.OrdinalIgnoreCase);

        mod = mod with { Local = mod.Local with { AlternateGuids = [.. mod.Local.AlternateGuids, .. alternateGuids] } };

        return mod;
    }

    private static Mod CreateModFromBepInPlugin(BepInPluginAttribute plugin, string dllPath)
    {
        var (author, name) = PluginNamingStrategy.ParseAuthorAndName(plugin.Name, plugin.Guid);

        var warnings = ModMetadataValidator.ValidateModMetadata(name, author, plugin.Version, plugin.Guid);

        return new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = plugin.Guid,
                FilePath = dllPath,
                IsServerMod = false,
                LocalName = name,
                LocalAuthor = author,
                LocalVersion = plugin.Version?.ToString() ?? "0.0.0",
                LocalSptVersion = null,
            },
            LoadWarnings = warnings,
        };
    }

    private static (string DllPath, BepInPluginAttribute Plugin) SelectprimaryPlugin(
        List<(string DllPath, BepInPluginAttribute Plugin)> plugins,
        string directoryName
    )
    {
        var normalizedDirName = PluginNamingStrategy.NormalizeModName(directoryName);

        var directoryMatch = plugins
            .Where(p =>
            {
                var fileName = Path.GetFileNameWithoutExtension(p.DllPath);
                return fileName.Equals(directoryName, StringComparison.OrdinalIgnoreCase)
                    || fileName.Contains(normalizedDirName, StringComparison.OrdinalIgnoreCase)
                    || PluginNamingStrategy
                        .NormalizeModName(fileName)
                        .Equals(normalizedDirName, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(p => Path.GetFileNameWithoutExtension(p.DllPath).Length)
            .FirstOrDefault();

        if (directoryMatch.Plugin is not null)
        {
            return directoryMatch;
        }

        var corePlugin = plugins
            .Where(p =>
            {
                var fileName = Path.GetFileNameWithoutExtension(p.DllPath);
                return fileName.Contains("Core", StringComparison.OrdinalIgnoreCase)
                    || p.Plugin.Name.Contains("Core", StringComparison.OrdinalIgnoreCase);
            })
            .FirstOrDefault();

        if (corePlugin.Plugin is not null)
        {
            return corePlugin;
        }

        return plugins
            .OrderBy(p => p.Plugin.Guid.Split('.').Length)
            .ThenBy(p => p.Plugin.Guid.Length)
            .ThenBy(p => p.Plugin.Name, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
