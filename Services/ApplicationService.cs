using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services;

/// <summary>
/// Main application service that orchestrates the SPT mod checking workflow.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ApplicationService(
    IInitializationService initializationService,
    ISptInstallationService sptInstallationService,
    IModScannerService modScannerService,
    IModReconciliationService modReconciliationService,
    IModMatchingService modMatchingService,
    IModEnrichmentService modEnrichmentService,
    IModDependencyService modDependencyService,
    IIgnoredUpdateStore ignoredUpdateStore,
    IRemoteIgnoreFileClient remoteIgnoreFileClient,
    IModCheckReporter reporter,
    ILogger<ApplicationService> logger,
    IModResolutionService modResolutionService,
    ICompatibilityValidationService compatibilityValidationService,
    IUpdateOrchestrationService updateOrchestrationService
) : IApplicationService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Mod>> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting mod check workflow");
        reporter.Banner();

        try
        {
            // Remove any legacy API key file from previous versions.
            await initializationService.RemoveLegacyApiKeyFileAsync();

            logger.LogDebug("Validating SPT path");
            var sptPath = initializationService.GetValidatedSptPath(args);
            if (sptPath is null)
            {
                logger.LogWarning("SPT path validation failed, exiting");
                return [];
            }

            logger.LogInformation("Using SPT path: {SptPath}", sptPath);

            logger.LogDebug("Validating SPT installation");
            var sptVersion = await ValidateSptInstallationAsync(sptPath, cancellationToken);
            if (sptVersion is null)
            {
                logger.LogWarning("SPT version validation failed, exiting");
                return [];
            }

            logger.LogInformation("SPT version validated: {SptVersion}", sptVersion);

            // Must run after the SPT update check.
            logger.LogDebug("Checking for Check Mods updates");
            await updateOrchestrationService.CheckForCheckModsUpdateAsync(sptVersion, cancellationToken);

            // Offer to refresh the local ignore list from the author-maintained remote list (opt-in, default no).
            logger.LogDebug("Offering remote ignore list refresh");
            await MaybeFetchRemoteIgnoresAsync(cancellationToken);

            reporter.Blank();
            reporter.Heading("Loading mods...");

            logger.LogDebug("Checking for improperly installed mods");
            reporter.Status("Checking mod installation locations...");
            var misplacedReport = await modScannerService.DetectMisplacedModsAsync(sptPath, cancellationToken);
            if (misplacedReport.Any)
            {
                logger.LogWarning(
                    "Found {WrongFolder} misplaced mods and {CrossInstalled} cross-installed directories; excluding them from the remaining checks and continuing",
                    misplacedReport.WrongFolder.Count,
                    misplacedReport.CrossInstalled.Count
                );

                // Surface the problem but keep running.
                reporter.MisplacedMods(misplacedReport);
            }

            logger.LogDebug("Scanning and reconciling mods");
            var mods = await ScanAndReconcileModsAsync(sptPath, sptVersion, misplacedReport, cancellationToken);
            if (mods.Count == 0)
            {
                logger.LogInformation("No mods remaining after reconciliation");
                reporter.Warning("No mods remaining after reconciliation.");
                return [];
            }

            logger.LogInformation("Found {ModCount} mods after reconciliation", mods.Count);

            logger.LogDebug("Matching mods with Forge API");
            mods = (await MatchModsWithApiAsync(mods, sptVersion, cancellationToken)).ToList();

            // Enrich matched mods with version data, then apply locally-stored update suppressions.
            logger.LogDebug("Enriching mods with version data");
            mods = (await EnrichModsWithVersionDataAsync(mods, sptVersion, cancellationToken)).ToList();

            logger.LogDebug("Applying ignored updates");
            var modsWithIgnores = await updateOrchestrationService.ApplyIgnoredUpdatesAsync(mods, cancellationToken);
            mods = modsWithIgnores.ToList();

            // Suppressed false positives are skipped.
            logger.LogDebug("Checking mod version compatibility");
            reporter.Blank();
            reporter.Heading("Checking mod version compatibility...");
            compatibilityValidationService.CheckModVersionCompatibility(mods, sptVersion);
            reporter.VersionCompatibilityResults(mods, sptVersion);

            logger.LogDebug("Checking mod dependencies");
            mods = (await CheckModDependenciesAsync(mods, cancellationToken)).ToList();

            logger.LogDebug("Displaying results");
            reporter.VersionTable(mods);

            logger.LogInformation("Mod check workflow completed successfully");
            return mods;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Operation was cancelled");
            reporter.Warning("Operation cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during mod check workflow");
            reporter.Exception(ex);
        }

        return [];
    }

    /// <summary>
    /// Offers to refresh the local ignore list from the author-maintained remote list. Opt-in with a default of "no",
    /// and skipped entirely when input is non-interactive or no remote URL is configured. Merges without overwriting
    /// existing local entries; any failure leaves the local list untouched.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task MaybeFetchRemoteIgnoresAsync(CancellationToken cancellationToken = default)
    {
        // Can't prompt without an interactive console (treated as "no"), and nothing to do without a configured URL.
        if (Console.IsInputRedirected || !remoteIgnoreFileClient.IsConfigured)
        {
            return;
        }

        reporter.Blank();
        reporter.Heading("Community ignore list...");

        if (reporter.PromptFetchRemoteIgnores())
        {
            var remote = await remoteIgnoreFileClient.FetchAsync(cancellationToken);
            if (remote is null)
            {
                reporter.RemoteIgnoresUnavailable();
            }
            else
            {
                reporter.RemoteIgnoresMerged(await ignoredUpdateStore.MergeWithoutOverwriteAsync(remote, cancellationToken));
            }
        }

        reporter.Blank();
        reporter.Rule();
    }

    /// <summary>
    /// Validates the SPT installation and returns the version.
    /// </summary>
    /// <param name="sptPath">Path to SPT installation.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>SPT version or null if validation failed.</returns>
    private async Task<SemanticVersioning.Version?> ValidateSptInstallationAsync(
        string sptPath,
        CancellationToken cancellationToken = default
    )
    {
        var sptVersion = await sptInstallationService.GetAndValidateSptVersionAsync(sptPath, cancellationToken);
        if (sptVersion is null)
        {
            return null;
        }

        reporter.SptVersionValidated(sptVersion.ToString());

        await updateOrchestrationService.CheckForSptUpdatesAsync(sptVersion, cancellationToken);

        reporter.Blank();
        reporter.Rule();
        reporter.Blank();
        return sptVersion;
    }

    /// <summary>
    /// Scans mods from disk and reconciles server/client components.
    /// </summary>
    /// <param name="sptPath">Path to SPT installation.</param>
    /// <param name="sptVersion">SPT version for API lookups.</param>
    /// <param name="misplacedReport">Incorrectly installed mods which should be excluded from further operations.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task<List<Mod>> ScanAndReconcileModsAsync(
        string sptPath,
        SemanticVersioning.Version sptVersion,
        MisplacedModReport misplacedReport,
        CancellationToken cancellationToken = default
    )
    {
        var (serverMods, clientMods) = await modScannerService.ScanAllModsAsync(sptPath, cancellationToken);

        // Drop any mods flagged as misplaced.
        if (misplacedReport.Any)
        {
            serverMods = ExcludeMisplacedMods(serverMods, misplacedReport);
            clientMods = ExcludeMisplacedMods(clientMods, misplacedReport);
        }

        if (serverMods.Count == 0 && clientMods.Count == 0)
        {
            logger.LogInformation("No mods found in SPT installation");
            reporter.NoModsFound();
            return [];
        }

        reporter.Success($"Loaded {serverMods.Count} server mods and {clientMods.Count} client mods.");

        // Fetch API info for mods with warnings.
        var modsWithWarnings = serverMods.Concat(clientMods).Where(m => m.HasWarnings).ToList();
        if (modsWithWarnings.Count > 0)
        {
            modsWithWarnings = (await modResolutionService.FetchSourceCodeUrlsForModsAsync(modsWithWarnings, sptVersion, cancellationToken)).ToList();
        }

        reporter.LoadingWarnings(modsWithWarnings);

        reporter.Blank();
        reporter.Rule();
        reporter.Blank();
        reporter.Heading("Reconciling mod components...");

        var result = modReconciliationService.ReconcileMods(serverMods, clientMods);

        // Fetch API info for mods with reconciliation warnings.
        var pairsWithNotes = result.ReconciledPairs.Where(p => p.Notes.Count > 0).ToList();
        if (pairsWithNotes.Count > 0)
        {
            var (updatedPairs, _) = await modResolutionService.FetchSourceCodeUrlsForPairedModsAsync(
                pairsWithNotes,
                sptVersion,
                cancellationToken
            );
            
            var newPairs = result.ReconciledPairs.ToList();
            foreach (var updatedPair in updatedPairs)
            {
                var idx = newPairs.FindIndex(p => p.SelectedMod.Local.Guid == updatedPair.SelectedMod.Local.Guid);
                if (idx >= 0)
                {
                    newPairs[idx] = updatedPair;
                }
            }
            result = new ModReconciliationResult { Mods = result.Mods, ReconciledPairs = newPairs, UnmatchedServerMods = result.UnmatchedServerMods, UnmatchedClientMods = result.UnmatchedClientMods };
        }

        reporter.ReconciliationResults(result);

        return result.Mods.ToList();
    }

    /// <summary>
    /// Returns the mods with any misplaced entries removed: those whose DLL path was flagged as misplaced, plus any
    /// inside a cross-installed directory whose intruder couldn't be identified (the whole folder is excluded).
    /// </summary>
    /// <param name="mods">The list of mods to filter.</param>
    /// <param name="report">The misplaced mods report containing exclusions.</param>
    private static List<Mod> ExcludeMisplacedMods(List<Mod> mods, MisplacedModReport report)
    {
        var excludedFiles = new HashSet<string>(report.ExcludedFilePaths, StringComparer.OrdinalIgnoreCase);
        var excludedDirectories = report.ExcludedDirectories;

        return mods.Where(mod =>
                !excludedFiles.Contains(mod.Local.FilePath)
                && !excludedDirectories.Any(directory => IsWithinDirectory(mod.Local.FilePath, directory))
            )
            .ToList();
    }

    /// <summary>
    /// Determines whether <paramref name="filePath"/> lives inside <paramref name="directory"/> (or is that directory
    /// itself), comparing fully-resolved paths case-insensitively.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="directory">The directory path to check against.</param>
    private static bool IsWithinDirectory(string filePath, string directory)
    {
        var fullFile = System.IO.Path.GetFullPath(filePath);
        var fullDirectory = System.IO.Path.GetFullPath(directory);

        if (string.Equals(fullFile, fullDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var prefix = fullDirectory.EndsWith(System.IO.Path.DirectorySeparatorChar)
            ? fullDirectory
            : fullDirectory + System.IO.Path.DirectorySeparatorChar;

        return fullFile.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Matches mods with the Forge API.
    /// </summary>
    /// <param name="mods">Mods to match.</param>
    /// <param name="sptVersion">SPT version for compatibility filtering.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task<IReadOnlyList<Mod>> MatchModsWithApiAsync(
        IReadOnlyList<Mod> mods,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        reporter.Blank();
        reporter.Heading($"Verifying Forge records for {mods.Count} mods...");

        var matchedMods = await reporter.RunForgeQueryProgressAsync(
            mods.Count,
            setValue =>
                modMatchingService.MatchModsAsync(
                    mods,
                    sptVersion,
                    (_, current, _) => setValue(current),
                    cancellationToken
                ),
            cancellationToken
        );

        reporter.Success("Forge verification complete!");
        reporter.Blank();

        // Display warnings for mods that couldn't be verified
        reporter.UnverifiedMods(matchedMods.ToList());

        reporter.Rule();
        
        return matchedMods;
    }

    /// <summary>
    /// Enriches matched mods with version data from the API.
    /// </summary>
    /// <param name="mods">Mods to enrich.</param>
    /// <param name="sptVersion">SPT version for compatibility filtering.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task<IReadOnlyList<Mod>> EnrichModsWithVersionDataAsync(
        IReadOnlyList<Mod> mods,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var matchedMods = mods.Where(m => m.IsMatched).ToList();

        if (matchedMods.Count == 0)
        {
            return mods;
        }

        var enrichedMods = await modEnrichmentService.EnrichAllWithVersionDataAsync(
            matchedMods, sptVersion, cancellationToken
        );

        var enrichedByGuid = enrichedMods.ToDictionary(m => m.Local.Guid, StringComparer.OrdinalIgnoreCase);

        var result = new List<Mod>(mods.Count);
        foreach (var mod in mods)
        {
            if (!string.IsNullOrWhiteSpace(mod.Local.Guid) && 
                enrichedByGuid.TryGetValue(mod.Local.Guid, out var enriched))
            {
                result.Add(enriched);
            }
            else
            {
                result.Add(mod);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Checks mod dependencies and displays a dependency tree with any issues.
    /// </summary>
    /// <param name="mods">Mods to check dependencies for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task<IReadOnlyList<Mod>> CheckModDependenciesAsync(IReadOnlyList<Mod> mods, CancellationToken cancellationToken = default)
    {
        if (!mods.Any(m => m.IsMatched))
        {
            return mods;
        }

        reporter.Blank();

        // Build set of installed mod GUIDs
        var installedGuids = mods.Where(m => !string.IsNullOrWhiteSpace(m.Local.Guid))
            .Select(m => m.Local.Guid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Count matched mods for progress
        var matchedCount = mods.Count(m => m.IsMatched && m.Api.ApiModId.HasValue);

        // Mods with an available update get a second dependency fetch (at the proposed version), deduped by API mod ID.
        // Include those in the progress total.
        var updatableCount = mods.Where(m =>
                m.IsMatched
                && m.Api.ApiModId.HasValue
                && m.Update.UpdateStatus == UpdateStatus.UpdateAvailable
                && !string.IsNullOrWhiteSpace(m.Update.LatestVersion)
            )
            .Select(m => m.Api.ApiModId!.Value)
            .Distinct()
            .Count();

        reporter.Heading($"Checking mod dependencies for {matchedCount} mods...");

        var (updatedMods, result) = await reporter.RunForgeQueryProgressAsync(
            matchedCount + updatableCount,
            setValue =>
                modDependencyService.AnalyzeDependenciesAsync(
                    mods,
                    installedGuids,
                    (current, _) => setValue(current),
                    cancellationToken
                ),
            cancellationToken
        );

        reporter.DependencyResults(result);
        return updatedMods;
    }
}
