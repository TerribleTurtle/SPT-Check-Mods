using CheckMods.Models;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// Extracts SPT Server Mod reflection.
/// </summary>
public interface IServerModExtractor
{
    /// <summary>
    /// Attempts to read the DLL as a server (SPT) mod. Returns the mod if SPT mod metadata is found, otherwise null.
    /// </summary>
    Task<Mod?> ExtractServerModMetadataAsync(string dllPath, CancellationToken cancellationToken = default);
}
