using System.Collections.Generic;

namespace CheckMods.Models;

/// <summary>
/// A BepInPlugin DLL plus the assembly-reference data used to group related DLLs.
/// </summary>
/// <param name="DllPath">The absolute path to the DLL file.</param>
/// <param name="Plugin">The BepInPlugin attribute extracted from the DLL.</param>
/// <param name="AssemblyName">The .NET assembly name.</param>
/// <param name="ReferencedAssemblyNames">The set of other assemblies referenced by this DLL.</param>
public sealed record PluginDll(
    string DllPath,
    BepInPluginAttribute Plugin,
    string? AssemblyName,
    IReadOnlySet<string> ReferencedAssemblyNames
);
