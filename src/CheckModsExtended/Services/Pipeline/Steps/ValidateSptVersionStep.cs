using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that validates the SPT version.
/// </summary>
public sealed class ValidateSptVersionStep(
    ISptInstallationService sptInstallationService,
    IUpdateOrchestrationService updateOrchestrationService,
    IModCheckReporter reporter,
    ILogger<ValidateSptVersionStep> logger
) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Validating SPT installation");
        context.SptVersion = await sptInstallationService.GetAndValidateSptVersionAsync(
            context.SptPath!,
            cancellationToken
        );

        if (context.SptVersion is null)
        {
            logger.LogWarning("SPT version validation failed, exiting");
            context.IsCancelled = true;
            return;
        }

        logger.LogInformation("SPT version validated: {SptVersion}", context.SptVersion);
        reporter.SptVersionValidated(context.SptVersion.ToString());

        await updateOrchestrationService.CheckForSptUpdatesAsync(context.SptVersion, cancellationToken);

        reporter.Blank();
        reporter.Rule();
        reporter.Blank();
    }
}
