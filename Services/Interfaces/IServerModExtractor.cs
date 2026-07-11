using CheckMods.Models;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// Extracts SPT Server Mod metadata from package.json.
/// </summary>
public interface IServerModExtractor
{
    /// <summary>
    /// Inspects a server mod DLL and extracts its metadata properties.
    /// </summary>
    /// <param name="dllPath">The absolute path to the server mod DLL.</param>
    /// <param name="sptDirectory">The absolute path to the SPT installation directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A populated Mod object, or null if the DLL is not a valid server mod.</returns>
    Task<Mod?> ExtractServerModMetadataAsync(
        string dllPath,
        string sptDirectory,
        CancellationToken cancellationToken = default
    );
}
