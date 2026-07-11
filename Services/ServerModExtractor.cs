using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services;

[Injectable(InjectionType.Transient)]
public sealed class ServerModExtractor(ILogger<ServerModExtractor> logger) : IServerModExtractor
{
    /// <inheritdoc />
    public Task<Mod?> ExtractServerModMetadataAsync(
        string dllPath,
        string sptDirectory,
        CancellationToken cancellationToken = default
    )
    {
        // Extraction is CPU-bound and synchronous, so we run it in a task to avoid blocking the calling thread.
        return Task.Run(() => ExtractServerModMetadata(dllPath, sptDirectory), cancellationToken);
    }

    private Mod? ExtractServerModMetadata(string dllPath, string sptDirectory)
    {
        var loadContext = new SptAssemblyLoadContext(sptDirectory);

        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(dllPath));
            var assembly = loadContext.LoadFromStream(stream);
            var metadata = LoadSptMetadataFromAssembly(assembly);

            if (metadata is null)
            {
                return null;
            }

            // Use reflection to access properties
            var metadataType = metadata.GetType();
            var modGuid = metadataType.GetProperty("ModGuid")?.GetValue(metadata)?.ToString();
            var name = metadataType.GetProperty("Name")?.GetValue(metadata)?.ToString();
            var author = metadataType.GetProperty("Author")?.GetValue(metadata)?.ToString();
            var modVersion = metadataType.GetProperty("Version")?.GetValue(metadata)?.ToString();
            var sptVersion = metadataType.GetProperty("SptVersion")?.GetValue(metadata)?.ToString();

            if (string.IsNullOrEmpty(modGuid))
            {
                return null;
            }

            var version = modVersion ?? GetAssemblyVersion(assembly);

            var warnings = ValidateModMetadata(name ?? string.Empty, author ?? string.Empty, version, modGuid);

            return new Mod
            {
                Local = new LocalModIdentity
                {
                    Guid = modGuid,
                    FilePath = dllPath,
                    IsServerMod = true,
                    LocalName = name ?? string.Empty,
                    LocalAuthor = author ?? string.Empty,
                    LocalVersion = version,
                    LocalSptVersion = sptVersion,
                },
                LoadWarnings = warnings,
            };
        }
        catch (Exception ex)
            when (ex
                    is IOException
                        or UnauthorizedAccessException
                        or System.Security.SecurityException
                        or BadImageFormatException
                        or FileLoadException
            )
        {
            logger.LogDebug(ex, "Could not inspect DLL as a server mod: {Path}", dllPath);
            return null;
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private static object? LoadSptMetadataFromAssembly(Assembly assembly)
    {
        var types = GetLoadableTypes(assembly);
        var metadataType = types.FirstOrDefault(t => t.BaseType?.Name == "AbstractModMetadata" && !t.IsAbstract);

        if (metadataType is null)
        {
            return null;
        }

        return Activator.CreateInstance(metadataType);
    }

    /// <summary>
    /// Gets all types from an assembly that can be loaded, gracefully handling types with missing dependencies.
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return only the types that loaded successfully (non-null entries)
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static string GetAssemblyVersion(Assembly assembly)
    {
        var infoVersionAttr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (infoVersionAttr is null)
        {
            var version = assembly.GetName().Version;
            if (version is null)
            {
                return string.Empty;
            }

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        var fullVersion = infoVersionAttr.InformationalVersion;
        if (string.IsNullOrEmpty(fullVersion))
        {
            var version = assembly.GetName().Version;
            if (version is null)
            {
                return string.Empty;
            }

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        var plusIndex = fullVersion.IndexOf('+');
        if (plusIndex > 0)
        {
            fullVersion = fullVersion[..plusIndex];
        }

        return fullVersion;
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
        return SemVer.TryParse(version, "ServerModExtractor").IsT0;
    }

    /// <summary>
    /// Custom AssemblyLoadContext that resolves SPT assemblies from the SPT directory.
    /// </summary>
    private sealed class SptAssemblyLoadContext(string sptDirectory) : AssemblyLoadContext(true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Try to find the assembly in the SPT directory
            var sptPath = Path.Combine(sptDirectory, "SPT", $"{assemblyName.Name}.dll");
            if (File.Exists(sptPath))
            {
                using var stream = new MemoryStream(File.ReadAllBytes(sptPath));
                return LoadFromStream(stream);
            }

            return null;
        }
    }
}
