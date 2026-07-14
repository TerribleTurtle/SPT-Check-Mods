using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
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
    private readonly IInitializationService _initializationService;

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
    /// <param name="initializationService">The service for resolving the SPT path.</param>
    public CheckModsCommand(
        IUpdateWorkflowOrchestrator orchestrator,
        IIgnoredUpdateWorkflow ignoredUpdateWorkflow,
        IUserPromptService userPromptService,
        CheckModsExtended.Utils.IProcessRunner processRunner,
        IPluginScanCache pluginScanCache,
        ICacheManager cacheManager,
        IScanCacheService scanCacheService,
        IModCheckReporter reporter,
        IInitializationService initializationService)
    {
        _orchestrator = orchestrator;
        _ignoredUpdateWorkflow = ignoredUpdateWorkflow;
        _userPromptService = userPromptService;
        _processRunner = processRunner;
        _pluginScanCache = pluginScanCache;
        _cacheManager = cacheManager;
        _scanCacheService = scanCacheService;
        _reporter = reporter;
        _initializationService = initializationService;
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
        var sptPath = _initializationService.GetValidatedSptPath(args);
        bool loadedFromCache = false;
        IReadOnlyList<Mod>? currentMods = null;

        var cache = await _scanCacheService.LoadCacheAsync(cancellationToken);
        bool isCacheValidForPath = cache != null && string.Equals(cache.SptPath, sptPath, StringComparison.OrdinalIgnoreCase);

        if (isCacheValidForPath && cache?.Response?.Mods != null && cache.Response.Mods.Count > 0 && _userPromptService.PromptLoadFromCache(cache.CachedAtUtc))
        {
            _reporter.CachedVersionTable(cache.Response.Mods);
            currentMods = cache.Response.Mods.Select(m => m.ToDomain()).ToList();
            loadedFromCache = true;
        }

        while (true)
        {
            if (!loadedFromCache)
            {
                var contextResult = await _orchestrator.RunPipelineAsync(args, cancellationToken);
                currentMods = contextResult?.Mods;
            }

            if (currentMods is not null || loadedFromCache)
            {
                var endOfRunChoice = await _ignoredUpdateWorkflow.RunAsync(currentMods, cancellationToken);

                if (endOfRunChoice == EndOfRunChoice.Rescan)
                {
                    loadedFromCache = false; // Next iteration will trigger a scan
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
