using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that displays the final results.
/// </summary>
public sealed class DisplayResultsStep(IModCheckReporter reporter, ILogger<DisplayResultsStep> logger, RuntimeConfig runtimeConfig) : IWorkflowStep
{
    /// <inheritdoc />
    public Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Displaying results");
        
        if (runtimeConfig.Format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var json = JsonSerializer.Serialize(context.Mods, CheckModsExtendedJsonSerializerContext.Default.IReadOnlyListMod);
#pragma warning disable Spectre1000 // Use AnsiConsole instead of System.Console
            Console.WriteLine(json);
#pragma warning restore Spectre1000 // Use AnsiConsole instead of System.Console
        }
        else if (runtimeConfig.Format.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
#pragma warning disable Spectre1000 // Use AnsiConsole instead of System.Console
            Console.WriteLine("CSV format is not yet implemented.");
#pragma warning restore Spectre1000 // Use AnsiConsole instead of System.Console
        }
        else
        {
            reporter.VersionTable(context.Mods);
        }
        
        logger.LogInformation("Mod check workflow completed successfully");

        return Task.CompletedTask;
    }
}
