using CheckModsExtended.Configuration;
using CheckModsExtended.Extensions;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace CheckModsExtended;

/// <summary>
/// Main entry point for the CheckModsExtended application.
/// </summary>
public sealed class Program
{
    private static CancellationTokenSource? _cts;
    private static bool _wasCancelled;

    /// <summary>
    /// Sets up dependency injection, runs the application, and handles any unhandled exceptions.
    /// </summary>
    /// <param name="args">Command line arguments. The only argument is the SPT installation path.</param>
    public static async Task<int> Main(string[] args)
    {
        int exitCode = 0;
        ILogger<Program>? logger = null;

        // Path reported at the end of the run; falls back to the static default if DI setup fails.
        var logFilePath = LoggingOptions.CurrentLogFilePath;

        _wasCancelled = false;
        _cts = new CancellationTokenSource();
        Console.CancelKeyPress += OnCancelKeyPress;

        ServiceProvider? serviceProvider = null;

        WindowsConsoleHelper.TryEnableVirtualTerminalProcessing();

        // Trimming breaks Spectre.Console's detection of terminal capabilities because it relies on P/Invokes.
        // We explicitly enabled VT processing on Windows above, so we can now safely force Spectre to emit ANSI codes.
        if (!Console.IsOutputRedirected)
        {
            AnsiConsole.Profile.Capabilities.Ansi = true;
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.Standard;
        }

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var services = new ServiceCollection();
            services.AddCheckModsExtendedServices(configuration);
            serviceProvider = services.BuildServiceProvider();

            logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("CheckModsExtended application starting. Args: {Args}", string.Join(", ", args));

            logFilePath = serviceProvider.GetRequiredService<IOptions<LoggingOptions>>().Value.LogFilePath;

            var applicationService = serviceProvider.GetRequiredService<IUpdateWorkflowOrchestrator>();
            var ignoredUpdateWorkflow = serviceProvider.GetRequiredService<IIgnoredUpdateWorkflow>();

            var mods = await applicationService.RunPipelineAsync(args, _cts.Token);

            if (mods is not null)
            {
                await ignoredUpdateWorkflow.RunAsync(mods, _cts.Token);
            }

            logger.LogInformation("CheckModsExtended application completed successfully");
        }
        catch (OperationCanceledException)
        {
            logger?.LogInformation("Application was cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogCritical(ex, "Unhandled exception occurred");
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
            exitCode = 2;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            _cts.Dispose();
            _cts = null;

            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine($"[grey]Check Mods v{VersionInfo.SemVer} (build {VersionInfo.GitHash})[/]");
            AnsiConsole.MarkupLine($"[grey]Log file: {logFilePath}[/]");

            // Prevent the console window from closing instantly when the user double-clicks the executable on Windows.
            // Console.IsInputRedirected will be true if the application is run from a script or piping environment,
            // in which case we don't want to block the thread.
            if (!_wasCancelled && !Console.IsInputRedirected)
            {
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(intercept: true);
                }

                AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
                Console.ReadKey();
            }

            if (serviceProvider is not null)
            {
                await serviceProvider.DisposeAsync();
            }
        }

        return exitCode;
    }

    /// <summary>
    /// Handles the Ctrl+C event to cancel the application.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The console cancel event arguments.</param>
    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _wasCancelled = true;
        _cts?.Cancel();
        AnsiConsole.MarkupLine("[yellow]Cancellation requested. Shutting down gracefully...[/]");
    }
}

