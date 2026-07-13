using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Groups plugin DLLs that belong to the same mod and separates those that don't.
/// </summary>
public interface IModPartitioner
{
    /// <summary>
    /// Groups plugin DLLs that belong to the same mod and separates those that don't.
    /// </summary>
    /// <param name="plugins">The plugins to partition.</param>
    /// <returns>A list of plugin groups, where each group belongs to the same mod.</returns>
    List<List<PluginDll>> PartitionByRelatedness(List<PluginDll> plugins);
}
