using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Extracts BepInEx metadata reflection.
/// </summary>
public interface IPluginMetadataExtractor
{
    /// <summary>
    /// Gets a list of valid client DLL files from the given plugins path.
    /// </summary>
    List<string> GetValidClientDllFiles(string pluginsPath);

    /// <summary>
    /// Processes client DLLs in parallel and returns valid mods.
    /// </summary>
    Task<List<Mod>> ProcessClientDllsInParallelAsync(
        List<string> dllFiles,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Turns a directory's DLLs into mods. DLLs that reference each other become one mod, while unrelated DLLs stay separate.
    /// </summary>
    Task<List<Mod>> ConsolidateDirectoryModsAsync(string directory, List<string> dllPaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to read the DLL as a client (BepInEx) mod. Returns the mod if a BepInPlugin attribute is found, otherwise null.
    /// </summary>
    Task<Mod?> TryDetectClientModAsync(string dllPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads plugin metadata and assembly references from each BepInPlugin DLL.
    /// </summary>
    Task<List<PluginDll>> ReadPluginDllsAsync(List<string> dllPaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes a relatedness component as a single mod, using its primary plugin for the name, GUID, and path.
    /// </summary>
    MisplacedMod ToMisplacedMod(List<PluginDll> group, string directoryName);
}

