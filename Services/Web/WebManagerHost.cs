using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using CheckModsExtended.Configuration;
using CheckModsExtended.Extensions;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace CheckModsExtended.Services.Web;

/// <summary>
/// Hosts the ASP.NET Core Web API for the Graphical User Interface (GUI).
/// </summary>
public static class WebManagerHost
{
    private const int BasePort = 37194;
    private const int MaxPort = 37204;

    public static WebApplication BuildApp(string[] args, Action<IServiceCollection>? configureTestServices = null)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // Map the existing configuration pipeline
        var runtimeConfig = new RuntimeConfig
        {
            IsHeadless = true, // Never prompt for CLI input when running the Web Manager
            IsVerbose = args.Contains("-v") || args.Contains("--verbose"),
            IsDebug = args.Contains("-d") || args.Contains("--debug"),
        };

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        if (!runtimeConfig.IsVerbose && !runtimeConfig.IsDebug)
        {
            builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        }

        // Inject the identical services the CLI uses
        builder.Services.AddCheckModsExtendedServices(configuration, runtimeConfig);
        builder.Services.AddSingleton(runtimeConfig);

        configureTestServices?.Invoke(builder.Services);

        // Configure JSON for Native AOT
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, CheckModsExtendedJsonSerializerContext.Default);
        });

        // Find a free port in our fallback range
        int port = FindAvailablePort(BasePort, MaxPort);

        builder.WebHost.UseKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, port);
        });

        var app = builder.Build();

        var embeddedProvider = new Microsoft.Extensions.FileProviders.EmbeddedFileProvider(
            System.Reflection.Assembly.GetExecutingAssembly(),
            "CheckModsExtended.wwwroot"
        );

        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = embeddedProvider });
        app.UseStaticFiles(new StaticFileOptions { FileProvider = embeddedProvider });

        WebEndpoints.MapEndpoints(app, args);

        return app;
    }

    /// <summary>
    /// Starts the Web UI and blocks until the application exits.
    /// </summary>
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken, Action<IServiceCollection>? configureTestServices = null)
    {
        AnsiConsole.MarkupLine("[grey]Starting CheckModsExtended Web Manager...[/]");

        var app = BuildApp(args, configureTestServices);

        // Hook into the start event to launch the browser
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var server = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>();
            var serverAddresses = server.Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
            string actualUrl = serverAddresses?.Addresses.FirstOrDefault() ?? $"http://127.0.0.1:{BasePort}";

            if (args.Contains("--from-cli"))
            {
                actualUrl += "?cli=1";
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]Web Manager is running at: {actualUrl}[/]");
            AnsiConsole.MarkupLine("[grey]Press Ctrl+C to shut down.[/]");

            var browserLauncher = app.Services.GetRequiredService<IBrowserLauncher>();
            var _ = browserLauncher.TryOpenUrl(actualUrl);
        });

        await app.RunAsync(cancellationToken);
    }

    private static int FindAvailablePort(int startingPort, int maxPort)
    {
        var activeTcpListeners = IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Select(e => e.Port)
            .ToHashSet();

        for (int port = startingPort; port <= maxPort; port++)
        {
            if (!activeTcpListeners.Contains(port))
            {
                return port;
            }
        }

        // Fallback to random dynamic port if entire range is taken
        return 0;
    }
}
