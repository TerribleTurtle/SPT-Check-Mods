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
    private readonly IUserPromptService _userPromptService;
    private readonly CheckModsExtended.Utils.IProcessRunner _processRunner;
    private readonly IPluginScanCache _pluginScanCache;
    private readonly ICacheManager _cacheManager;
    private readonly IScanCacheService _scanCacheService;
    private readonly IModCheckReporter _reporter;

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
    /// <param name="userPromptService">The service for interacting with the user.</param>
    /// <param name="processRunner">The service for running external processes.</param>
    /// <param name="pluginScanCache">The cache for plugin scanning.</param>
    /// <param name="cacheManager">The manager for application caching.</param>
    /// <param name="scanCacheService">The service for managing scan caches.</param>
    /// <param name="reporter">The reporter for presenting mod check results.</param>
    public CheckModsCommand(
        IUpdateWorkflowOrchestrator orchestrator,
        IIgnoredUpdateWorkflow ignoredUpdateWorkflow,
        IUserPromptService userPromptService,
        CheckModsExtended.Utils.IProcessRunner processRunner,
        IPluginScanCache pluginScanCache,
        ICacheManager cacheManager,
        IScanCacheService scanCacheService,
        IModCheckReporter reporter)
    {
        _orchestrator = orchestrator;
        _ignoredUpdateWorkflow = ignoredUpdateWorkflow;
        _userPromptService = userPromptService;
        _processRunner = processRunner;
        _pluginScanCache = pluginScanCache;
        _cacheManager = cacheManager;
        _scanCacheService = scanCacheService;
        _reporter = reporter;
    }

    /// <summary>
    /// Executes the main check mods command asynchronously.
    /// Handles caching, running the update pipeline, and process inception for the Web GUI.
    /// Process Inception details: If the user chooses to open the GUI at the end of the run,
    /// the CLI restarts its own executable but passes the "gui" argument. This creates a detached
    /// child process running the web dashboard, allowing the current CLI process to exit cleanly.
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

        var cache = await _scanCacheService.LoadCacheAsync(cancellationToken);
        if (cache != null && _userPromptService.PromptLoadFromCache(cache.CachedAtUtc))
        {
            _reporter.CachedVersionTable(cache.Response.Mods);
            return 0;
        }

        while (true)
        {
            var contextResult = await _orchestrator.RunPipelineAsync(args, cancellationToken);

            if (contextResult?.Mods is not null)
            {
                var endOfRunChoice = await _ignoredUpdateWorkflow.RunAsync(contextResult.Mods, cancellationToken);

                if (endOfRunChoice == EndOfRunChoice.Rescan)
                {
                    _pluginScanCache.Clear();
                    _cacheManager.Clear();
                    continue;
                }

                if (endOfRunChoice == EndOfRunChoice.LaunchWebGui)
                {
                    // "Process inception": The CLI restarts its own executable but passes the "gui" argument.
                    // This creates a detached child process running the web dashboard, allowing the current
                    // CLI process to exit cleanly without keeping the terminal blocked.
                    var processPath = System.Environment.ProcessPath;
                    if (processPath != null)
                    {
                        var guiArgs = string.IsNullOrWhiteSpace(settings.SptPath) ? "gui" : $"gui \"{settings.SptPath}\"";
                        var startInfo = new System.Diagnostics.ProcessStartInfo(processPath, guiArgs)
                        {
                            UseShellExecute = true
                        };
                        _processRunner.Start(startInfo);
                    }
                    break;
                }
            }

            break;
        }

        return 0; // Success
    }
}
