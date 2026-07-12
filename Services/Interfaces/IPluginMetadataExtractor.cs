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
    /// <param name="pluginsPath">The path to the plugins directory.</param>
    /// <returns>A list of valid client DLL file paths.</returns>
    List<string> GetValidClientDllFiles(string pluginsPath);

    /// <summary>
    /// Processes client DLLs in parallel and returns valid mods.
    /// </summary>
    /// <param name="dllFiles">The list of DLL file paths to process.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of valid mods detected from the DLLs.</returns>
    Task<List<Mod>> ProcessClientDllsInParallelAsync(
        List<string> dllFiles,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Turns a directory's DLLs into mods. DLLs that reference each other become one mod, while unrelated DLLs stay separate.
    /// </summary>
    /// <param name="directory">The directory containing the DLLs.</param>
    /// <param name="dllPaths">The paths of the DLLs within the directory.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A tuple containing <c>Mods</c> (the consolidated mods) and <c>Plugins</c> (the individual plugin DLL metadata).</returns>
    Task<(List<Mod> Mods, List<PluginDll> Plugins)> ConsolidateDirectoryModsAsync(
        string directory,
        List<string> dllPaths,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Attempts to read the DLL as a client (BepInEx) mod. Returns the mod if a BepInPlugin attribute is found, otherwise null.
    /// </summary>
    /// <param name="dllPath">The path to the DLL to detect.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The detected mod if a BepInPlugin attribute is found, otherwise null.</returns>
    Task<Mod?> TryDetectClientModAsync(string dllPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads plugin metadata and assembly references from each BepInPlugin DLL.
    /// </summary>
    /// <param name="dllPaths">The paths of the DLLs to read.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of plugin metadata and assembly references for each valid DLL.</returns>
    Task<List<PluginDll>> ReadPluginDllsAsync(List<string> dllPaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes a relatedness component as a single mod, using its primary plugin for the name, GUID, and path.
    /// </summary>
    /// <param name="group">The relatedness group of plugins.</param>
    /// <param name="directoryName">The name of the directory containing the plugins.</param>
    /// <returns>A misplaced mod representing the relatedness component.</returns>
    MisplacedMod ToMisplacedMod(List<PluginDll> group, string directoryName);
}
