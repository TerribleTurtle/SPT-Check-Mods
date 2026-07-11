using System.Net.Http.Headers;
using System.Reflection;
using CheckModsExtended.Configuration;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Decorators;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Services.UI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Serilog;
using Serilog.Events;
using SPTarkov.DI;

namespace CheckModsExtended.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all CheckModsExtended services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    public static IServiceCollection AddCheckModsExtendedServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<ForgeApiOptions>(configuration.GetSection("ForgeApiOptions"));

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

        services.AddTransient<IBrowserLauncher, BrowserLauncher>();
        services.AddTransient<ICompatibilityValidationService, CompatibilityValidationService>();
        services.AddTransient<IIgnoredUpdateWorkflow, IgnoredUpdateWorkflow>();
        services.AddTransient<IInitializationService, InitializationService>();
        services.AddTransient<IMisplacedModDetector, MisplacedModDetector>();
        services.AddTransient<IModEnrichmentService, ModEnrichmentService>();
        services.AddTransient<IModLookupStrategy, ModLookupStrategy>();
        services.AddTransient<IModMatchingService, ModMatchingService>();
        services.AddTransient<IModReconciliationService, ModReconciliationService>();
        services.AddTransient<IModResolutionService, ModResolutionService>();
        services.AddTransient<IModScannerService, ModScannerService>();
        services.AddTransient<IUpdateWorkflowOrchestrator, UpdateWorkflowOrchestrator>();
        services.AddTransient<IModPartitioner, ModPartitioner>();
        services.AddTransient<IPluginMetadataExtractor, PluginMetadataExtractor>();
        services.AddTransient<IServerModExtractor, ServerModExtractor>();
        services.AddTransient<ISptInstallationService, SptInstallationService>();
        services.AddTransient<IUpdateCheckService, UpdateCheckService>();
        services.AddTransient<IUpdateOrchestrationService, UpdateOrchestrationService>();

        services.AddSingleton<IIgnoredUpdateStore, IgnoredUpdateStore>();
        services.AddSingleton<IModCheckReporter, SpectreModCheckReporter>();
        services.AddSingleton<IDependencyUiRenderer, DependencyUiRenderer>();
        services.AddSingleton<IMisplacedModUiRenderer, MisplacedModUiRenderer>();
        services.AddSingleton<IProgressRenderer, ProgressRenderer>();
        services.AddSingleton<IReconciliationUiRenderer, ReconciliationUiRenderer>();
        services.AddSingleton<ITableRenderer, TableRenderer>();
        services.AddSingleton<ITextRenderer, TextRenderer>();
        services.AddSingleton<IVersionTableUiRenderer, VersionTableUiRenderer>();

        // Register the named HttpClient for ForgeApi
        var httpClientBuilder = services.AddHttpClient(
            "ForgeApi",
            (serviceProvider, client) =>
            {
                // Disable HttpClient timeout to allow Polly's TotalRequestTimeout (5 minutes) to control the lifecycle
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

                var version = CheckModsExtended.Utils.VersionInfo.SemVer;
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CheckModsExtended", version));
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("(+https://github.com/TerribleTurtle/CheckModsExtended)")
                );
            }
        );
        
        httpClientBuilder.AddResilienceHandler("pacer", builder =>
        {
            var rateLimiter = new System.Threading.RateLimiting.TokenBucketRateLimiter(
                new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
                {
                    TokenLimit = 5,
                    TokensPerPeriod = 1,
                    ReplenishmentPeriod = TimeSpan.FromMilliseconds(333),
                    QueueLimit = 10_000,
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    AutoReplenishment = true
                });

            builder.AddRateLimiter(new Polly.RateLimiting.RateLimiterStrategyOptions 
            { 
                RateLimiter = args => rateLimiter.AcquireAsync(1, args.Context.CancellationToken)
            });
        });
        
        httpClientBuilder.AddStandardResilienceHandler(options =>
        {
            // Allow up to 5 minutes total to survive 429 Retry-After delays
            options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
            
            // Prevent the Circuit Breaker from tripping during heavy rate limiting
            options.CircuitBreaker.FailureRatio = 0.99;
            options.CircuitBreaker.MinimumThroughput = 1000;
        });

        services.AddTransient<IForgeApiClient, ForgeApiClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("ForgeApi");
            return ActivatorUtilities.CreateInstance<ForgeApiClient>(sp, httpClient);
        });

        services.AddTransient<IForgeApiService, CachedForgeApiService>(sp =>
        {
            var inner = ActivatorUtilities.CreateInstance<ForgeApiService>(sp);
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

                var version = CheckModsExtended.Utils.VersionInfo.SemVer;
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CheckModsExtended", version));
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("(+https://github.com/TerribleTurtle/CheckModsExtended)")
                );
            }
        ).AddStandardResilienceHandler();

        services.AddTransient<IWorkflowStep, RemoveLegacyApiKeyStep>();
        services.AddTransient<IWorkflowStep, ValidateSptPathStep>();
        services.AddTransient<IWorkflowStep, ValidateSptVersionStep>();
        services.AddTransient<IWorkflowStep, CheckModsExtendedUpdateCheckStep>();
        services.AddTransient<IWorkflowStep, MaybeFetchRemoteIgnoresStep>();
        services.AddTransient<IWorkflowStep, DetectMisplacedModsStep>();
        services.AddTransient<IWorkflowStep, ScanAndReconcileModsStep>();
        services.AddTransient<IWorkflowStep, MatchModsWithApiStep>();
        services.AddTransient<IWorkflowStep, EnrichModsWithVersionDataStep>();
        services.AddTransient<IWorkflowStep, ApplyIgnoredUpdatesStep>();
        services.AddTransient<IWorkflowStep, CheckModVersionCompatibilityStep>();
        services.AddTransient<IWorkflowStep, CheckModDependenciesStep>();
        services.AddTransient<IWorkflowStep, DisplayResultsStep>();

        return services;
    }
}


