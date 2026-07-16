using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
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
    private readonly IPluginScanCache _pluginScanCache;
    private readonly ICacheManager _cacheManager;

    /// <summary>
    /// Command line settings.
    /// </summary>
    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "[SptPath]")]
        [Description("The path to your SPT installation directory. Defaults to the current directory.")]
        public string? SptPath { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckModsCommand"/> class.
    /// </summary>
    /// <param name="orchestrator">The orchestrator for the update workflow.</param>
    /// <param name="ignoredUpdateWorkflow">The workflow for managing ignored updates.</param>
    /// <param name="pluginScanCache">The cache for plugin scanning.</param>
    /// <param name="cacheManager">The manager for application caching.</param>
    public CheckModsCommand(
        IUpdateWorkflowOrchestrator orchestrator,
        IIgnoredUpdateWorkflow ignoredUpdateWorkflow,
        IPluginScanCache pluginScanCache,
        ICacheManager cacheManager)
    {
        _orchestrator = orchestrator;
        _ignoredUpdateWorkflow = ignoredUpdateWorkflow;
        _pluginScanCache = pluginScanCache;
        _cacheManager = cacheManager;
    }

    /// <summary>
    /// Executes the main check mods command asynchronously.
    /// Handles caching, running the update pipeline, and delegating to the Web GUI if requested.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous execution operation. The task result contains the exit code.</returns>
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        var args = string.IsNullOrWhiteSpace(settings.SptPath) ? Array.Empty<string>() : new[] { settings.SptPath };
        IReadOnlyList<Mod>? currentMods = null;

        while (true)
        {
            var contextResult = await _orchestrator.RunPipelineAsync(args, cancellationToken);
            currentMods = contextResult?.Mods;

            if (currentMods is not null)
            {
                var endOfRunChoice = await _ignoredUpdateWorkflow.RunAsync(currentMods, cancellationToken);

                if (endOfRunChoice == EndOfRunChoice.Rescan)
                {
                    _pluginScanCache.Clear();
                    _cacheManager.Clear();
                    continue;
                }

                if (endOfRunChoice == EndOfRunChoice.LaunchWebGui)
                {
                    return ExitCodes.LaunchWebGui;
                }

                if (endOfRunChoice == EndOfRunChoice.Exit)
                {
                    return ExitCodes.ExitRequested;
                }
            }

            break;
        }

        return ExitCodes.Success;
    }
}
