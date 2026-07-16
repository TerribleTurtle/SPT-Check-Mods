using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that validates the SPT installation path.
/// </summary>
public sealed class ValidateSptPathStep(
    IInitializationService initializationService,
    ILogger<ValidateSptPathStep> logger
) : IWorkflowStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Validating SPT path");
        context.SptPath = initializationService.GetValidatedSptPath(context.Args);

        if (context.SptPath is null)
        {
            logger.LogWarning("SPT path validation failed, exiting");
            context.IsCancelled = true;
            return Task.CompletedTask;
        }

        logger.LogInformation("Using SPT path: {SptPath}", context.SptPath);
        return Task.CompletedTask;
    }
}
