using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;


namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that checks mod version compatibility.
/// </summary>

public sealed class CheckModVersionCompatibilityStep(
    ICompatibilityValidationService compatibilityValidationService,
    IModCheckReporter reporter,
    ILogger<CheckModVersionCompatibilityStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking mod version compatibility");
        reporter.Blank();
        reporter.Heading("Checking mod version compatibility...");
        
        compatibilityValidationService.CheckModVersionCompatibility(context.Mods, context.SptVersion!);
        reporter.VersionCompatibilityResults(context.Mods, context.SptVersion!);

        return Task.CompletedTask;
    }
}

