using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that checks for Check Mods tool updates.
/// </summary>
public sealed class CheckModsExtendedUpdateCheckStep(
    IUpdateOrchestrationService updateOrchestrationService,
    ILogger<CheckModsExtendedUpdateCheckStep> logger
) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking for Check Mods updates");
        await updateOrchestrationService.CheckForCheckModsExtendedUpdateAsync(context.SptVersion!, cancellationToken);
    }
}
