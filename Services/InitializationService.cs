using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Implementation of <see cref="IInitializationService"/>.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class InitializationService(
    IModCheckReporter reporter,
    ILogger<InitializationService> logger,
    Microsoft.Extensions.Options.IOptions<CheckModsExtended.Configuration.AppPaths> appPaths,
    IFileSystem fileSystem
) : IInitializationService
{
    /// <inheritdoc />
    public async Task RemoveLegacyApiKeyFileAsync()
    {
        try
        {
            var configDirectory = Path.GetFullPath(appPaths.Value.AppDataDirectory);
            var configFilePath = Path.GetFullPath(Path.Combine(configDirectory, "apikey.txt"));

            if (!fileSystem.FileExists(configFilePath))
            {
                return;
            }

            await Task.Run(() => fileSystem.DeleteFile(configFilePath));
            logger.LogInformation("Removed legacy API key file.");
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Failed to remove legacy API key file due to IO error");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Failed to remove legacy API key file due to access permissions");
        }
    }

    /// <inheritdoc />
    public string? GetValidatedSptPath(string[] args)
    {
        reporter.Banner();
        reporter.Heading("Validating SPT installation...");

        if (args.Length == 0)
        {
            var currentPath = fileSystem.GetCurrentDirectory();
            reporter.UsingPath(currentPath);
            return currentPath;
        }

        var safePath = SecurityHelper.GetSafePath(args[0]);
        if (safePath is null)
        {
            reporter.Error("Error: Invalid path provided.");
            return null;
        }

        if (!fileSystem.DirectoryExists(safePath))
        {
            reporter.DirectoryDoesNotExist(safePath);
            return null;
        }

        reporter.UsingPath(safePath);
        return safePath;
    }
}
