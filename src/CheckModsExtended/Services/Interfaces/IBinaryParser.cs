using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

public record ServerModBinaryInfo(
    string? Guid,
    string? Name,
    string? Author,
    string? Version,
    string? SptVersion,
    string? Url
);

public interface IBinaryParser
{
    BepInPluginAttribute? ExtractBepInPlugin(string dllPath);
    (BepInPluginAttribute? Plugin, string? AssemblyName, HashSet<string>? ReferencedNames) ReadPluginDllInfo(string dllPath);
    ServerModBinaryInfo? ExtractServerModMetadata(string dllPath);
}
