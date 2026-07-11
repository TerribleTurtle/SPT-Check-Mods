using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that checks mod version compatibility.
/// </summary>
[Injectable(InjectionType.Transient)]
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
