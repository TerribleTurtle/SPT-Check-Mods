using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services;

[Injectable(InjectionType.Transient)]
public sealed class PluginMetadataExtractor(
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

        return PartitionByRelatedness(allPlugins).Select(group => CreateConsolidatedMod(group, directoryName)).ToList();
    }

    /// <inheritdoc />
    public async Task<Mod?> TryDetectClientModAsync(string dllPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
            using var loadContext = CreateMetadataLoadContext(dllPath);
            var assembly = loadContext.LoadFromByteArray(bytes);

            foreach (var type in GetLoadableTypes(assembly))
            {
                try
                {
                    var plugin = ExtractBepInPluginAttribute(type);
                    if (plugin is not null)
                    {
                        return CreateModFromBepInPlugin(plugin, dllPath);
                    }
                }
                catch (Exception ex) when (ex is TypeLoadException or BadImageFormatException or ReflectionTypeLoadException or IOException or UnauthorizedAccessException)
                {
                    logger.LogDebug(ex, "Could not inspect type");
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
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We are inspecting dynamically loaded mod assemblies, not application code.")]
    public async Task<List<PluginDll>> ReadPluginDllsAsync(List<string> dllPaths, CancellationToken cancellationToken = default)
    {
        List<PluginDll> plugins = [];

        foreach (var dllPath in dllPaths)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
                using var loadContext = CreateMetadataLoadContext(dllPath);
                var assembly = loadContext.LoadFromByteArray(bytes);
                var plugin = ScanAssemblyForBepInPluginAttribute(assembly);

                if (plugin is null)
                {
                    continue;
                }

                var referencedNames = assembly
                    .GetReferencedAssemblies()
                    .Select(name => name.Name)
                    .OfType<string>()
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                plugins.Add(new PluginDll(dllPath, plugin, assembly.GetName().Name, referencedNames));
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
    public List<List<PluginDll>> PartitionByRelatedness(List<PluginDll> plugins)
    {
        var parents = Enumerable.Range(0, plugins.Count).ToArray();

        int Find(int node)
        {
            while (parents[node] != node)
            {
                parents[node] = parents[parents[node]];
                node = parents[node];
            }

            return node;
        }

        for (var i = 0; i < plugins.Count; i++)
        {
            for (var j = i + 1; j < plugins.Count; j++)
            {
                if (AreRelated(plugins[i], plugins[j]))
                {
                    parents[Find(i)] = Find(j);
                }
            }
        }

        return plugins
            .Select((plugin, index) => (plugin, Root: Find(index)))
            .GroupBy(item => item.Root)
            .Select(group => group.Select(item => item.plugin).ToList())
            .ToList();
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

    private async Task<Mod?> ExtractClientModMetadataAsync(string dllPath, ConcurrentBag<(string FileName, string Reason)> warnings, CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
            using var loadContext = CreateMetadataLoadContext(dllPath);
            var assembly = loadContext.LoadFromByteArray(bytes);

            return ScanAssemblyForBepInPlugin(assembly, dllPath);
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

    private static MetadataLoadContext CreateMetadataLoadContext(string dllPath)
    {
        var resolver = new AssemblyResolver(dllPath);
        return new MetadataLoadContext(resolver);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We are inspecting dynamically loaded mod assemblies, not application code.")]
    private Mod? ScanAssemblyForBepInPlugin(Assembly assembly, string dllPath)
    {
        foreach (var type in assembly.GetTypes())
        {
            try
            {
                var bepInPlugin = ExtractBepInPluginAttribute(type);
                if (bepInPlugin is null)
                {
                    continue;
                }

                return CreateModFromBepInPlugin(bepInPlugin, dllPath);
            }
            catch (Exception ex) when (ex is TypeLoadException or BadImageFormatException or ReflectionTypeLoadException or IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(ex, "Skipping type due to load exception");
            }
        }

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We are inspecting dynamically loaded mod assemblies, not application code.")]
    private BepInPluginAttribute? ScanAssemblyForBepInPluginAttribute(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            try
            {
                var bepInPlugin = ExtractBepInPluginAttribute(type);
                if (bepInPlugin is null)
                {
                    continue;
                }

                return bepInPlugin;
            }
            catch (Exception ex) when (ex is TypeLoadException or BadImageFormatException or ReflectionTypeLoadException or IOException or UnauthorizedAccessException)
            {
                logger.LogDebug(ex, "Skipping type due to load exception");
            }
        }
        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We are inspecting dynamically loaded mod assemblies, not application code.")]
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static BepInPluginAttribute? ExtractBepInPluginAttribute(Type type)
    {
        var customAttributes = type.GetCustomAttributesData();

        var bepInPluginAttribute = customAttributes.FirstOrDefault(attr =>
            attr.AttributeType.Name is "BepInPlugin" or "BepInPluginAttribute"
            || (attr.AttributeType.FullName?.Contains("BepInPlugin") ?? false)
        );

        if (bepInPluginAttribute is null || bepInPluginAttribute.ConstructorArguments.Count < 3)
        {
            return null;
        }

        var guid = bepInPluginAttribute.ConstructorArguments[0].Value?.ToString() ?? "";
        var name = bepInPluginAttribute.ConstructorArguments[1].Value?.ToString() ?? "";
        var version = bepInPluginAttribute.ConstructorArguments[2].Value?.ToString() ?? "";

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

    private static bool AreRelated(PluginDll a, PluginDll b)
    {
        return References(a, b) || References(b, a) || SameAuthorNamespace(a.Plugin.Guid, b.Plugin.Guid);
    }

    private static bool References(PluginDll from, PluginDll to)
    {
        return !string.IsNullOrEmpty(to.AssemblyName) && from.ReferencedAssemblyNames.Contains(to.AssemblyName);
    }

    private static readonly HashSet<string> _genericGuidSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "com",
        "org",
        "net",
        "io",
        "dev",
        "co",
        "app",
        "me",
        "gg",
        "xyz",
        "github",
        "gitlab",
        "gitee",
    };

    private static bool SameAuthorNamespace(string guidA, string guidB)
    {
        var nsA = AuthorNamespaceSegments(guidA);
        var nsB = AuthorNamespaceSegments(guidB);

        if (nsA.Count == 0 || nsB.Count == 0)
        {
            return false;
        }

        var min = Math.Min(nsA.Count, nsB.Count);

        var shared = 0;
        while (shared < min && string.Equals(nsA[shared], nsB[shared], StringComparison.OrdinalIgnoreCase))
        {
            shared++;
        }

        if (shared < min)
        {
            return false;
        }

        return nsA.Take(shared).Any(segment => !_genericGuidSegments.Contains(segment));
    }

    private static List<string> AuthorNamespaceSegments(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            return [];
        }

        var parts = guid.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length <= 1 ? [] : parts[..^1].ToList();
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
