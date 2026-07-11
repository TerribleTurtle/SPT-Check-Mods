using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;


namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that displays the final results.
/// </summary>

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

