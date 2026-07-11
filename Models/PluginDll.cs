using System.Collections.Generic;

namespace CheckMods.Models;

/// <summary>
/// A BepInPlugin DLL plus the assembly-reference data used to group related DLLs.
/// </summary>
public sealed record PluginDll(
    string DllPath,
    BepInPluginAttribute Plugin,
    string? AssemblyName,
    IReadOnlySet<string> ReferencedAssemblyNames
);
