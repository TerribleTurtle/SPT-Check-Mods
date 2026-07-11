using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Logging;


namespace CheckMods.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that checks for Check Mods tool updates.
/// </summary>

public sealed class CheckModsUpdateCheckStep(
    IUpdateOrchestrationService updateOrchestrationService,
    ILogger<CheckModsUpdateCheckStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking for Check Mods updates");
        await updateOrchestrationService.CheckForCheckModsUpdateAsync(context.SptVersion!, cancellationToken);
    }
}
