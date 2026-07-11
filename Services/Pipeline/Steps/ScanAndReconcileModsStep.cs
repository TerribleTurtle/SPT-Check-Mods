using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Logging;


namespace CheckMods.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that scans and reconciles mods.
/// </summary>

public sealed class ScanAndReconcileModsStep(
    IModScannerService modScannerService,
    IModResolutionService modResolutionService,
    IModReconciliationService modReconciliationService,
    IModCheckReporter reporter,
    ILogger<ScanAndReconcileModsStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Scanning and reconciling mods");
        
        var (serverMods, clientMods) = await modScannerService.ScanAllModsAsync(context.SptPath!, cancellationToken);

        if (context.MisplacedReport is not null && context.MisplacedReport.Any)
        {
            serverMods = ExcludeMisplacedMods(serverMods, context.MisplacedReport);
            clientMods = ExcludeMisplacedMods(clientMods, context.MisplacedReport);
        }

        if (serverMods.Count == 0 && clientMods.Count == 0)
        {
            logger.LogInformation("No mods found in SPT installation");
            reporter.NoModsFound();
            context.IsCancelled = true;
            return;
        }

        reporter.Success($"Loaded {serverMods.Count} server mods and {clientMods.Count} client mods.");

        var modsWithWarnings = serverMods.Concat(clientMods).Where(m => m.HasWarnings).ToList();
        if (modsWithWarnings.Count > 0)
        {
            modsWithWarnings = (await modResolutionService.FetchSourceCodeUrlsForModsAsync(modsWithWarnings, context.SptVersion!, cancellationToken)).ToList();
        }

        reporter.LoadingWarnings(modsWithWarnings);

        reporter.Blank();
        reporter.Rule();
        reporter.Blank();
        reporter.Heading("Reconciling mod components...");

        var result = modReconciliationService.ReconcileMods(serverMods, clientMods);

        var pairsWithNotes = result.ReconciledPairs.Where(p => p.Notes.Count > 0).ToList();
        if (pairsWithNotes.Count > 0)
        {
            var (updatedPairs, _) = await modResolutionService.FetchSourceCodeUrlsForPairedModsAsync(
                pairsWithNotes,
                context.SptVersion!,
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

        context.Mods = result.Mods.ToList();
        
        if (context.Mods.Count == 0)
        {
            logger.LogInformation("No mods remaining after reconciliation");
            reporter.Warning("No mods remaining after reconciliation.");
            context.IsCancelled = true;
            return;
        }

        logger.LogInformation("Found {ModCount} mods after reconciliation", context.Mods.Count);
    }

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
}
