using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Mono.Cecil;
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

[Injectable(InjectionType.Transient)]
public sealed class PluginMetadataExtractor(
    IModPartitioner partitioner,
    IOptions<ModScannerOptions> options,
    ILogger<PluginMetadataExtractor> logger
) : IPluginMetadataExtractor
{
    private readonly ModScannerOptions _options = options.Value;

    /// <inheritdoc />
    public List<string> GetValidClientDllFiles(string pluginsPath)
    {
        var sptDir = Path.Combine(pluginsPath, "spt");

        return Directory
            .GetFiles(pluginsPath, "*.dll", SearchOption.AllDirectories)
            .Where(file => !file.StartsWith(sptDir, StringComparison.OrdinalIgnoreCase))
            .Where(file => new FileInfo(file).Length <= _options.MaxDllSizeBytes)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<Mod>> ProcessClientDllsInParallelAsync(
        List<string> dllFiles,
        CancellationToken cancellationToken = default
    )
    {
        var warnings = new ConcurrentBag<(string FileName, string Reason)>();
        var results = new ConcurrentBag<Mod?>();

        await Parallel.ForEachAsync(
            dllFiles,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (dllPath, ct) =>
            {
                var mod = await ExtractClientModMetadataAsync(dllPath, warnings, ct);
                results.Add(mod);
            });

        foreach (var (fileName, reason) in warnings)
        {
            logger.LogDebug("Could not extract client mod metadata from {FileName}: {Reason}", fileName, reason);
        }

        var mods = results.Where(r => r is not null).Cast<Mod>().ToList();
        return FilterDuplicateClientMods(mods);
    }

    private static List<Mod> FilterDuplicateClientMods(List<Mod> mods)
    {
        return mods.DistinctBy(m => (m.Local.LocalName.ToLowerInvariant(), m.Local.LocalAuthor.ToLowerInvariant()))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<Mod>> ConsolidateDirectoryModsAsync(string directory, List<string> dllPaths, CancellationToken cancellationToken = default)
    {
        var allPlugins = await ReadPluginDllsAsync(dllPaths, cancellationToken);

        if (allPlugins.Count == 0)
        {
            return [];
        }

        var directoryName = Path.GetFileName(directory);

        return partitioner.PartitionByRelatedness(allPlugins).Select(group => CreateConsolidatedMod(group, directoryName)).ToList();
    }

    /// <inheritdoc />
    public async Task<Mod?> TryDetectClientModAsync(string dllPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
            using var stream = new MemoryStream(bytes);
            using var module = ModuleDefinition.ReadModule(stream);

            foreach (var type in module.Types)
            {
                var plugin = ExtractBepInPluginAttribute(type);
                if (plugin is not null)
                {
                    return CreateModFromBepInPlugin(plugin, dllPath);
                }
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
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<List<PluginDll>> ReadPluginDllsAsync(List<string> dllPaths, CancellationToken cancellationToken = default)
    {
        List<PluginDll> plugins = [];

        foreach (var dllPath in dllPaths)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
                using var stream = new MemoryStream(bytes);
                using var module = ModuleDefinition.ReadModule(stream);
                
                BepInPluginAttribute? plugin = null;
                foreach (var type in module.Types)
                {
                    plugin = ExtractBepInPluginAttribute(type);
                    if (plugin is not null)
                    {
                        break;
                    }
                }

                if (plugin is null)
                {
                    continue;
                }

                var referencedNames = module.AssemblyReferences
                    .Select(r => r.Name)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                plugins.Add(new PluginDll(dllPath, plugin, module.Assembly.Name.Name, referencedNames));
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
            }
        }

        return plugins;
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

    private static async Task<Mod?> ExtractClientModMetadataAsync(string dllPath, ConcurrentBag<(string FileName, string Reason)> warnings, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
            using var stream = new MemoryStream(bytes);
            using var module = ModuleDefinition.ReadModule(stream);

            foreach (var type in module.Types)
            {
                var plugin = ExtractBepInPluginAttribute(type);
                if (plugin is not null)
                {
                    return CreateModFromBepInPlugin(plugin, dllPath);
                }
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

    private static BepInPluginAttribute? ExtractBepInPluginAttribute(TypeDefinition type)
    {
        if (!type.HasCustomAttributes)
        {
            return null;
        }

        var attr = type.CustomAttributes.FirstOrDefault(a => 
            a.AttributeType.Name == "BepInPlugin" || 
            a.AttributeType.Name == "BepInPluginAttribute" ||
            a.AttributeType.FullName.Contains("BepInPlugin"));

        if (attr is null || !attr.HasConstructorArguments || attr.ConstructorArguments.Count < 3)
        {
            return null;
        }

        var guid = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var name = attr.ConstructorArguments[1].Value?.ToString() ?? "";
        var version = attr.ConstructorArguments[2].Value?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        return new BepInPluginAttribute(guid, name, version);
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
        var (author, name) = ParseAuthorAndName(plugin.Name, plugin.Guid);

        var warnings = ValidateModMetadata(name, author, plugin.Version, plugin.Guid);

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
        var normalizedDirName = NormalizeModName(directoryName);

        var directoryMatch = plugins
            .Where(p =>
            {
                var fileName = Path.GetFileNameWithoutExtension(p.DllPath);
                return fileName.Equals(directoryName, StringComparison.OrdinalIgnoreCase)
                    || fileName.Contains(normalizedDirName, StringComparison.OrdinalIgnoreCase)
                    || NormalizeModName(fileName).Equals(normalizedDirName, StringComparison.OrdinalIgnoreCase);
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

    private static string NormalizeModName(string name)
    {
        var dashIndex = name.IndexOf('-');
        if (dashIndex > 0 && dashIndex < name.Length - 1)
        {
            name = name[(dashIndex + 1)..];
        }

        string[] suffixes = ["Client", "Plugin", "Mod", "BepInEx"];
        var matchingSuffix = suffixes.FirstOrDefault(s => name.EndsWith(s, StringComparison.OrdinalIgnoreCase));
        if (matchingSuffix is not null)
        {
            name = name[..^matchingSuffix.Length];
        }

        return name.Trim();
    }

    private static (string Author, string Name) ParseAuthorAndName(string pluginName, string guid)
    {
        if (pluginName.Contains(" - "))
        {
            var parts = pluginName.Split(" - ", 2);
            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }
        }

        if (pluginName.Contains(" by ", StringComparison.OrdinalIgnoreCase))
        {
            var byIndex = pluginName.IndexOf(" by ", StringComparison.OrdinalIgnoreCase);
            var name = pluginName[..byIndex].Trim();
            var author = pluginName[(byIndex + 4)..].Trim();
            return (author, name);
        }

        var guidParts = guid.Split('.');
        if (guidParts.Length < 2)
        {
            return ("Unknown", pluginName);
        }

        var potentialAuthor = guidParts.Length >= 3 ? guidParts[^2] : guidParts[0];

        if (
            string.Equals(potentialAuthor, "com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(potentialAuthor, "org", StringComparison.OrdinalIgnoreCase)
            || string.Equals(potentialAuthor, "spt", StringComparison.OrdinalIgnoreCase)
            || string.Equals(potentialAuthor, "aki", StringComparison.OrdinalIgnoreCase)
        )
        {
            return ("Unknown", pluginName);
        }

        return (potentialAuthor, pluginName);
    }

    private static List<string> ValidateModMetadata(string name, string author, string version, string guid)
    {
        List<string> warnings = [];

        if (string.IsNullOrWhiteSpace(name))
        {
            warnings.Add("Missing mod name");
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            warnings.Add("Missing author");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            warnings.Add("Missing version");
        }
        else if (!IsValidVersion(version))
        {
            warnings.Add($"Invalid version format: {version}");
        }

        if (string.IsNullOrWhiteSpace(guid))
        {
            warnings.Add("Missing GUID");
        }

        return warnings;
    }

    private static bool IsValidVersion(string version)
    {
        return SemVer.TryParse(version, "PluginMetadataExtractor").IsT0;
    }
}


