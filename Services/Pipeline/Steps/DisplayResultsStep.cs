using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that displays the final results.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class DisplayResultsStep(
    IModCheckReporter reporter,
    ILogger<DisplayResultsStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Displaying results");
        reporter.VersionTable(context.Mods);
        logger.LogInformation("Mod check workflow completed successfully");
        
        return Task.CompletedTask;
    }
}
