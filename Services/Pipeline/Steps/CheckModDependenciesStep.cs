using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that checks mod dependencies.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class CheckModDependenciesStep(
    IModDependencyService modDependencyService,
    IModCheckReporter reporter,
    ILogger<CheckModDependenciesStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking mod dependencies");

        if (!context.Mods.Any(m => m.IsMatched))
        {
            return;
        }

        reporter.Blank();

        var installedGuids = context.Mods.Where(m => !string.IsNullOrWhiteSpace(m.Local.Guid))
            .Select(m => m.Local.Guid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchedCount = context.Mods.Count(m => m.IsMatched && m.Api.ApiModId.HasValue);

        var updatableCount = context.Mods.Where(m =>
                m.IsMatched
                && m.Api.ApiModId.HasValue
                && m.Update.UpdateStatus == UpdateStatus.UpdateAvailable
                && !string.IsNullOrWhiteSpace(m.Update.LatestVersion)
            )
            .Select(m => m.Api.ApiModId!.Value)
            .Distinct()
            .Count();

        reporter.Heading($"Checking mod dependencies for {matchedCount} mods...");

        var (updatedMods, result) = await reporter.RunForgeQueryProgressAsync(
            matchedCount + updatableCount,
            setValue =>
                modDependencyService.AnalyzeDependenciesAsync(
                    context.Mods,
                    installedGuids,
                    (current, _) => setValue(current),
                    cancellationToken
                ),
            cancellationToken
        );

        reporter.DependencyResults(result);
        context.Mods = updatedMods.ToList();
    }
}
