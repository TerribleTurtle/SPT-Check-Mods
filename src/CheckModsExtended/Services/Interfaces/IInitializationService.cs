namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles application initialization tasks such as validation and cleanup.
/// </summary>
public interface IInitializationService
{
    /// <summary>
    /// Removes the legacy API key file written by previous versions. Best-effort: any failure is logged and ignored.
    /// </summary>
    Task RemoveLegacyApiKeyFileAsync();

    /// <summary>
    /// Validates and returns the SPT installation path from arguments or current directory.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Validated SPT path or null if validation failed.</returns>
    string? GetValidatedSptPath(string[] args);
}
