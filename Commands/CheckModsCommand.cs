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
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;
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

    public CheckModsCommand(
        IUpdateWorkflowOrchestrator orchestrator,
        IIgnoredUpdateWorkflow ignoredUpdateWorkflow,
        IUserPromptService userPromptService,
        CheckModsExtended.Utils.IProcessRunner processRunner,
        IPluginScanCache pluginScanCache,
        Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache,
        IScanCacheService scanCacheService,
        IModCheckReporter reporter)
    {
        _orchestrator = orchestrator;
        _ignoredUpdateWorkflow = ignoredUpdateWorkflow;
        _userPromptService = userPromptService;
        _processRunner = processRunner;
        _pluginScanCache = pluginScanCache;
        _memoryCache = memoryCache;
        _scanCacheService = scanCacheService;
        _reporter = reporter;
    }

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
                    if (_memoryCache is Microsoft.Extensions.Caching.Memory.MemoryCache concreteCache)
                    {
                        concreteCache.Clear();
                    }
                    continue;
                }

                if (endOfRunChoice == EndOfRunChoice.LaunchWebGui)
                {
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
