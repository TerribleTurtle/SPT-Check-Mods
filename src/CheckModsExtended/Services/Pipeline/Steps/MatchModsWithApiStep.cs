using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that matches mods with the Forge API.
/// </summary>
public sealed class MatchModsWithApiStep(
    IModMatchingService modMatchingService,
    IModCheckReporter reporter,
    ILogger<MatchModsWithApiStep> logger
) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Matching mods with Forge API");
        reporter.Blank();
        reporter.Heading($"Verifying Forge records for {context.Mods.Count} mods...");

        var matchedMods = await reporter.RunForgeQueryProgressAsync(
            context.Mods.Count,
            setValue =>
                modMatchingService.MatchModsAsync(
                    context.Mods,
                    context.SptVersion!,
                    new Progress<int>(current => setValue(current)),
                    cancellationToken
                ),
            cancellationToken
        );

        reporter.Success("Forge verification complete!");
        reporter.Blank();

        reporter.UnverifiedMods(matchedMods.ToList());
        reporter.Rule();

        context.Mods = matchedMods.ToList();
    }
}
