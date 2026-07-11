using System.Net.Http.Headers;
using System.Reflection;
using CheckMods.Configuration;
using CheckMods.Services;
using CheckMods.Services.Decorators;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using SPTarkov.DI;

namespace CheckMods.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all CheckMods services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    public static IServiceCollection AddCheckModsServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ForgeApiOptions>(configuration.GetSection("ForgeApiOptions"));
        services.Configure<RateLimitOptions>(configuration.GetSection("RateLimitOptions"));
        services.Configure<ModScannerOptions>(configuration.GetSection("ModScannerOptions"));
        services.Configure<LoggingOptions>(configuration.GetSection("LoggingOptions"));
        services.Configure<UpdateCheckOptions>(configuration.GetSection("UpdateCheckOptions"));
        services.Configure<IgnoredUpdateOptions>(configuration.GetSection("IgnoredUpdateOptions"));

        services.AddMemoryCache();

        services.AddLogging(builder =>
        {
            // Suppress verbose HttpClient logging.
            builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

            var loggingOptions =
                configuration.GetSection("LoggingOptions").Get<LoggingOptions>() ?? new LoggingOptions();

            if (loggingOptions.EnableFileLogging)
            {
                LogEventLevel serilogLevel = loggingOptions.MinimumLogLevel switch
                {
                    LogLevel.Trace => LogEventLevel.Verbose,
                    LogLevel.Debug => LogEventLevel.Debug,
                    LogLevel.Information => LogEventLevel.Information,
                    LogLevel.Warning => LogEventLevel.Warning,
                    LogLevel.Error => LogEventLevel.Error,
                    LogLevel.Critical => LogEventLevel.Fatal,
                    _ => LogEventLevel.Information,
                };

                var serilogLogger = new Serilog.LoggerConfiguration()
                    .MinimumLevel.Is(serilogLevel)
                    .WriteTo.File(
                        loggingOptions.LogFilePath,
                        fileSizeLimitBytes: loggingOptions.MaxFileSizeBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: loggingOptions.RetainedFileCount,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                    )
                    .CreateLogger();

                builder.AddSerilog(serilogLogger, dispose: true);
            }
        });

        var diHandler = new DependencyInjectionHandler(services);
        diHandler.AddInjectableTypesFromAssembly(Assembly.GetExecutingAssembly());
        diHandler.InjectAll();

        // Register the named HttpClient for ForgeApi
        services.AddHttpClient(
            "ForgeApi",
            (serviceProvider, client) =>
            {
                var rateLimitOptions = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(rateLimitOptions.RequestTimeoutSeconds);

                var version = CheckMods.Utils.VersionInfo.SemVer;
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SPT-Check-Mods", version));
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("(+https://github.com/TerribleTurtle/SPT-Check-Mods)")
                );
            }
        ).AddStandardResilienceHandler();

        services.AddTransient<IForgeApiService, CachedForgeApiService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ForgeApi");
            var inner = ActivatorUtilities.CreateInstance<ForgeApiService>(sp, httpClient);
            return new CachedForgeApiService(
                inner,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedForgeApiService>>()
            );
        });

        services.AddTransient<IModDependencyService, CachedModDependencyService>(sp =>
        {
            var inner = ActivatorUtilities.CreateInstance<ModDependencyService>(sp);
            return new CachedModDependencyService(
                inner,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedModDependencyService>>()
            );
        });

        // Register the remote ignore-list client as a typed HttpClient.
        services.AddHttpClient<IRemoteIgnoreFileClient, RemoteIgnoreFileClient>(
            (serviceProvider, client) =>
            {
                var ignoredOptions = serviceProvider.GetRequiredService<IOptions<IgnoredUpdateOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(ignoredOptions.RemoteTimeoutSeconds);

                var version = CheckMods.Utils.VersionInfo.SemVer;
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SPT-Check-Mods", version));
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("(+https://github.com/TerribleTurtle/SPT-Check-Mods)")
                );
            }
        ).AddStandardResilienceHandler();

        return services;
    }
}
