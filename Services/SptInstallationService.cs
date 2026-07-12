using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Service responsible for SPT installation validation.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class SptInstallationService(
    ISptVersionClient forgeApiService,
    IModScannerService scannerService,
    IModCheckReporter reporter,
    ILogger<SptInstallationService> logger,
    IFileSystem fileSystem
) : ISptInstallationService
{
    /// <inheritdoc />
    public async Task<SemanticVersioning.Version?> GetAndValidateSptVersionAsync(
        string sptPath,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Validating SPT installation at: {SptPath}", sptPath);

        var coreDllPath = Path.Combine(sptPath, "SPT", "SPTarkov.Server.Core.dll");
        if (!fileSystem.FileExists(coreDllPath))
        {
            logger.LogError("SPT core DLL not found: {CoreDllPath}", coreDllPath);
            reporter.Error(
                "Error: Could not find SPT installation. Run this file in your root SPT directory, or provide the SPT path as an argument."
            );
            return null;
        }

        var localSptVersionStr = scannerService.GetSptVersion(sptPath);

        if (string.IsNullOrWhiteSpace(localSptVersionStr))
        {
            logger.LogError("Could not extract SPT version from core DLL");
            reporter.Error("Error: SPT version not found in SPTarkov.Server.Core.dll.");
            return null;
        }

        logger.LogDebug("Found local SPT version: {SptVersion}", localSptVersionStr);

        reporter.ValidatingSptVersion(localSptVersionStr);

        var validationResult = await forgeApiService.ValidateSptVersionAsync(localSptVersionStr, cancellationToken);

        var isValid = validationResult.Match(
            valid => valid,
            _ => false, // InvalidSptVersion
            _ => false // ApiError
        );

        if (!isValid)
        {
            validationResult.Switch(
                _ => { },
                _ => reporter.Error("Failed. SPT version not recognized by Forge API."),
                apiError => reporter.Error($"Failed. API error: {apiError.Message}")
            );

            return null;
        }

        var localSptVersionResult = SemVer.TryParse(localSptVersionStr, "SptInstallationService");
        if (!localSptVersionResult.TryPickT0(out var localSptVersion, out _))
        {
            logger.LogError("Could not parse SPT version '{SptVersion}'", localSptVersionStr);
            reporter.Error($"Failed. Could not parse SPT version '{localSptVersionStr}'.");
            return null;
        }

        reporter.Success("OK");
        return localSptVersion;
    }

    /// <inheritdoc />
    public async Task<List<SptVersionResult>> CheckForSptUpdatesAsync(
        SemanticVersioning.Version currentVersion,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Checking for SPT updates. Current version: {CurrentVersion}", currentVersion);

        var versionsResult = await forgeApiService.GetAllSptVersionsAsync(cancellationToken);

        return versionsResult.Match(
            versions =>
            {
                // Filter to versions newer than the current version, skipping any that can't be parsed.
                var newerVersions = versions
                    .Select(v =>
                    {
                        var parsed = SemVer.TryParse(v.Version, "SptUpdateCheck");
                        return (Raw: v, ParsedResult: parsed);
                    })
                    .Where(x => x.ParsedResult.TryPickT0(out var ver, out _) && ver > currentVersion)
                    .OrderByDescending(x => x.ParsedResult.AsT0)
                    .Select(x => x.Raw)
                    .ToList();

                logger.LogDebug("Found {Count} newer SPT versions", newerVersions.Count);
                return newerVersions;
            },
            apiError =>
            {
                logger.LogWarning("Failed to check for SPT updates: {Error}", apiError.Message);
                return [];
            }
        );
    }
}
