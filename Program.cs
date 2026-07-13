using CheckModsExtended.Commands;
using CheckModsExtended.Configuration;
using CheckModsExtended.Extensions;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended;

/// <summary>
/// Exit codes for the application.
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int Error = 2;
}

/// <summary>
/// Main entry point for the CheckModsExtended application.
/// </summary>
public sealed class Program
{
    private static CancellationTokenSource? _cts;
    private static bool _wasCancelled;

    /// <summary>
    /// Exposes the global cancellation token for commands.
    /// </summary>
    public static CancellationToken CancellationToken
    {
        get { return _cts?.Token ?? CancellationToken.None; }
    }

    /// <summary>
    /// Sets up dependency injection, determines the execution mode (CLI or Web GUI),
    /// runs the application, and handles any unhandled exceptions globally.
    /// </summary>
    /// <param name="args">
    /// Command line arguments passed to the application. 
    /// 
    /// Global Options:
    ///   -v, --verbose           Enables verbose logging output.
    ///   -d, --debug             Enables debug logging output, including full stack traces.
    ///   -y, --no-prompt         Runs in headless mode, skipping interactive prompts and inferring defaults.
    ///   -f, --format <TYPE>     Sets the output format (table, json, csv). Defaults to table.
    /// 
    /// Execution Modes:
    ///   gui [args]              Launches the local web UI dashboard. If specified, this must be the first argument.
    ///   cli [args]              Explicitly runs the application in CLI mode.
    /// 
    /// Commands:
    ///   [SptPath]               (Default Command) Checks for mod updates in the specified SPT path (or current directory).
    ///   list [SptPath]          Lists locally installed mods without checking for updates.
    ///   license                 Displays the application license.
    ///   clean                   Clears the application data directory (configuration overrides, ignored updates, logs).
    ///   diag                    Zips and exports the application's log files for easy sharing.
    ///   ignore                  Manages the ignored updates list (subcommands: list, add, remove).
    /// </param>
    /// <returns>An exit code representing the execution result (<see cref="ExitCodes"/>).</returns>
    [System.Diagnostics.CodeAnalysis.DynamicDependency(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All,
        typeof(Commands.CheckModsCommand)
    )]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All,
        typeof(Commands.CheckModsCommand.Settings)
    )]
    [System.Diagnostics.CodeAnalysis.DynamicDependency(
        System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All,
        typeof(Commands.GlobalSettings)
    )]
    public static async Task<int> Main(string[] args)
    {
        int exitCode = ExitCodes.Success;

        _wasCancelled = false;
        _cts = new CancellationTokenSource();
        Console.CancelKeyPress += OnCancelKeyPress;

        WindowsConsoleHelper.TryEnableVirtualTerminalProcessing();

        if (!Console.IsOutputRedirected)
        {
            AnsiConsole.Profile.Capabilities.Ansi = true;
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.Standard;
        }

        RuntimeConfig? runtimeConfig = null;
        string? finalLogPath = null;

        try
        {
            runtimeConfig = new RuntimeConfig
            {
                IsVerbose = args.Contains("-v") || args.Contains("--verbose"),
                IsDebug = args.Contains("-d") || args.Contains("--debug"),
            };
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var loggingOptions =
                configuration.GetSection("LoggingOptions").Get<LoggingOptions>() ?? new LoggingOptions();
            var appPaths = configuration.GetSection("AppPaths").Get<AppPaths>() ?? new AppPaths();

            finalLogPath = loggingOptions.LogFilePath;
            if (!string.IsNullOrEmpty(finalLogPath) && !Path.IsPathRooted(finalLogPath))
            {
                finalLogPath = Path.Combine(appPaths.AppDataDirectory, "logs", finalLogPath);
            }

            // Determine execution mode
            bool runGui = args.Length > 0 && args[0].Equals("gui", StringComparison.OrdinalIgnoreCase);

            if (runGui)
            {
                var webArgs = args.Skip(1).ToArray();
                await WebManagerHost.RunAsync(webArgs, CancellationToken);
            }
            else
            {
                var cliArgs = args;
                if (args.Length > 0 && args[0].Equals("cli", StringComparison.OrdinalIgnoreCase))
                {
                    cliArgs = args.Skip(1).ToArray();
                }

                var services = new ServiceCollection();
                services.AddCheckModsExtendedServices(configuration, runtimeConfig);

                services.AddSingleton(runtimeConfig);

                var registrar = new TypeRegistrar(services);
                var app = new CommandApp<CheckModsCommand>(registrar);

                app.Configure(config =>
                {
                    config.SetApplicationName("CheckModsExtended");
                    config.SetApplicationVersion(VersionInfo.SemVer);
                    config.SetInterceptor(new CheckModsInterceptor(runtimeConfig));

                    config
                        .AddCommand<ListModsCommand>("list")
                        .WithDescription("List locally installed mods without checking for updates.");

                    config.AddCommand<LicenseCommand>("license").WithDescription("Display the application license.");

                    config
                        .AddCommand<CleanCommand>("clean")
                        .WithDescription(
                            "Manage local app data (clears configuration overrides, ignored updates, and logs)."
                        );

                    config
                        .AddCommand<DiagCommand>("diag")
                        .WithDescription("Zip and export the application's log files from AppData for easy sharing.");

                    config.AddBranch(
                        "ignore",
                        ignore =>
                        {
                            ignore.SetDescription("Manage the ignored updates list.");
                            ignore.AddCommand<IgnoreListCommand>("list").WithDescription("List all ignored updates.");
                            ignore.AddCommand<IgnoreAddCommand>("add").WithDescription("Manually ignore an update.");
                            ignore.AddCommand<IgnoreRemoveCommand>("remove").WithDescription("Remove an ignored update.");
                        }
                    );

                    config.PropagateExceptions(); // Let the try-catch block handle exceptions
                });

                exitCode = await app.RunAsync(cliArgs);
            }
        }
        catch (OperationCanceledException)
        {
            // Usually cancelled is a zero exit code
            exitCode = ExitCodes.Success;
        }
        catch (Exception ex)
        {
            if (runtimeConfig?.IsDebug == true)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]A fatal error occurred:[/] {ex.Message.EscapeMarkup()}");
                if (finalLogPath != null)
                {
                    AnsiConsole.MarkupLine(
                        $"[grey]Please check the log file for the full stack trace: {finalLogPath.EscapeMarkup()}[/]"
                    );
                }

                string reportUrl = CrashReportUrl.Build(ex, VersionInfo.SemVer);
                AnsiConsole.MarkupLine("[yellow]You can report this crash on GitHub:[/]");
                AnsiConsole.MarkupLine($"[link]{reportUrl.EscapeMarkup()}[/]");
            }
            exitCode = ExitCodes.Error;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            _cts.Dispose();
            _cts = null;

            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine($"[grey]Check Mods v{VersionInfo.SemVer} (build {VersionInfo.GitHash})[/]");
            if (finalLogPath != null)
            {
                AnsiConsole.MarkupLine($"[grey]Log file: {finalLogPath}[/]");
            }

            var isHeadless =
                (runtimeConfig?.IsHeadless == true)
                || Console.IsInputRedirected
                || args.Contains("--no-prompt")
                || args.Contains("-y");

            if (!_wasCancelled && !isHeadless)
            {
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(intercept: true);
                }

                AnsiConsole.MarkupLine("[grey]Press any key to exit...[/]");
                Console.ReadKey();
            }
        }

        return exitCode;
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        _wasCancelled = true;
        _cts?.Cancel();
        AnsiConsole.MarkupLine("[yellow]Cancellation requested. Shutting down gracefully...[/]");
    }
}
