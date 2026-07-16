using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that applies ignored updates.
/// </summary>
public sealed class ApplyIgnoredUpdatesStep(
    IUpdateOrchestrationService updateOrchestrationService,
    ILogger<ApplyIgnoredUpdatesStep> logger
) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Applying ignored updates");
        var modsWithIgnores = await updateOrchestrationService.ApplyIgnoredUpdatesAsync(
            context.Mods,
            cancellationToken
        );
        context.Mods = modsWithIgnores.ToList();
    }
}
