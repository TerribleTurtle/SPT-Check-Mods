using CheckModsExtended.Commands;
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
using Spectre.Console.Cli;

namespace CheckModsExtended;

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
    /// Sets up dependency injection, runs the application, and handles any unhandled exceptions.
    /// </summary>
    /// <param name="args">Command line arguments. The only argument is the SPT installation path.</param>
    public static async Task<int> Main(string[] args)
    {
        int exitCode = 0;

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

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            services.AddCheckModsExtendedServices(configuration);
            
            runtimeConfig = new RuntimeConfig();
            services.AddSingleton(runtimeConfig);
            
            var registrar = new TypeRegistrar(services);
            var app = new CommandApp<CheckModsCommand>(registrar);

            app.Configure(config =>
            {
                config.SetApplicationName("check-mods");
                config.SetApplicationVersion(VersionInfo.SemVer);
                config.SetInterceptor(new CheckModsInterceptor(runtimeConfig));
                
                config.AddCommand<ListModsCommand>("list")
                    .WithDescription("List locally installed mods without checking for updates.");

                config.AddBranch("ignore", ignore =>
                {
                    ignore.SetDescription("Manage the ignored updates list.");
                    ignore.AddCommand<IgnoreListCommand>("list").WithDescription("List all ignored updates.");
                    ignore.AddCommand<IgnoreAddCommand>("add").WithDescription("Manually ignore an update.");
                    ignore.AddCommand<IgnoreRemoveCommand>("remove").WithDescription("Remove an ignored update.");
                });

                config.PropagateExceptions(); // Let the try-catch block handle exceptions
            });

            exitCode = await app.RunAsync(args);
        }
        catch (OperationCanceledException)
        {
            // Usually cancelled is a zero exit code
            exitCode = 0; 
        }
        catch (Exception ex)
        {
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
            if (LoggingOptions.CurrentLogFilePath != null)
            {
                AnsiConsole.MarkupLine($"[grey]Log file: {LoggingOptions.CurrentLogFilePath}[/]");
            }

            var isHeadless = runtimeConfig?.IsHeadless ?? (Console.IsInputRedirected || args.Contains("--no-prompt") || args.Contains("-y"));

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

