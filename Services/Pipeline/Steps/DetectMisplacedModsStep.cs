using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that detects misplaced mods.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class DetectMisplacedModsStep(
    IModScannerService modScannerService,
    IModCheckReporter reporter,
    ILogger<DetectMisplacedModsStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        reporter.Blank();
        reporter.Heading("Loading mods...");

        logger.LogDebug("Checking for improperly installed mods");
        reporter.Status("Checking mod installation locations...");
        
        context.MisplacedReport = await modScannerService.DetectMisplacedModsAsync(context.SptPath!, cancellationToken);
        if (context.MisplacedReport.Any)
        {
            logger.LogWarning(
                "Found {WrongFolder} misplaced mods and {CrossInstalled} cross-installed directories; excluding them from the remaining checks and continuing",
                context.MisplacedReport.WrongFolder.Count,
                context.MisplacedReport.CrossInstalled.Count
            );

            reporter.MisplacedMods(context.MisplacedReport);
        }
    }
}
