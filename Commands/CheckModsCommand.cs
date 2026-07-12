using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// The main execution command for the application.
/// </summary>
public sealed class CheckModsCommand : AsyncCommand<CheckModsCommand.Settings>
{
    private readonly IUpdateWorkflowOrchestrator _orchestrator;
    private readonly IIgnoredUpdateWorkflow _ignoredUpdateWorkflow;

    /// <summary>
    /// Command line settings.
    /// </summary>
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "[SptPath]")]
        [Description("The path to your SPT installation directory. Defaults to the current directory.")]
        public string? SptPath { get; set; }
    }

    public CheckModsCommand(IUpdateWorkflowOrchestrator orchestrator, IIgnoredUpdateWorkflow ignoredUpdateWorkflow)
    {
        _orchestrator = orchestrator;
        _ignoredUpdateWorkflow = ignoredUpdateWorkflow;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        var args = string.IsNullOrWhiteSpace(settings.SptPath) ? Array.Empty<string>() : new[] { settings.SptPath };

        var contextResult = await _orchestrator.RunPipelineAsync(args, cancellationToken);

        if (contextResult?.Mods is not null)
        {
            await _ignoredUpdateWorkflow.RunAsync(contextResult.Mods, cancellationToken);
        }

        return 0; // Success
    }
}
