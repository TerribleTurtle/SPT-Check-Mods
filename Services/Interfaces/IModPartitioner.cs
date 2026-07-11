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
    List<List<PluginDll>> PartitionByRelatedness(List<PluginDll> plugins);
}
