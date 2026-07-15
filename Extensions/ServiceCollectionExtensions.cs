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
    /// <param name="runtimeConfig">The runtime configuration.</param>
    public static IServiceCollection AddCheckModsExtendedServices(
        this IServiceCollection services,
        IConfiguration configuration,
        RuntimeConfig runtimeConfig
    )
    {
        services.Configure<ForgeApiOptions>(configuration.GetSection("ForgeApiOptions"));
        services.Configure<ModMatchingOptions>(configuration.GetSection("ModMatchingOptions"));

        services.Configure<ModScannerOptions>(configuration.GetSection("ModScannerOptions"));
        services.Configure<AppPaths>(configuration.GetSection("AppPaths"));
        services.Configure<LoggingOptions>(configuration.GetSection("LoggingOptions"));
        services.Configure<UpdateCheckOptions>(configuration.GetSection("UpdateCheckOptions"));
        services.Configure<IgnoredUpdateOptions>(configuration.GetSection("IgnoredUpdateOptions"));

        services.AddMemoryCache();

        services.AddInfrastructureLogging(configuration, runtimeConfig);
        services.AddCheckModsHttpClients();

        services.AddTransient<CheckModsExtended.Utils.IFileSystem, CheckModsExtended.Utils.FileSystem>();
        services.AddTransient<CheckModsExtended.Utils.IProcessRunner, CheckModsExtended.Utils.ProcessRunner>();
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
        services.AddTransient<ISettingsService, SettingsService>();
        services.AddTransient<IUpdateWorkflowOrchestrator, UpdateWorkflowOrchestrator>();
        services.AddTransient<IModPartitioner, ModPartitioner>();
        services.AddTransient<IPluginMetadataExtractor, PluginMetadataExtractor>();
        services.AddTransient<IServerModExtractor, ServerModExtractor>();
        services.AddTransient<ISptInstallationService, SptInstallationService>();
        services.AddTransient<IUpdateCheckService, UpdateCheckService>();
        services.AddTransient<IUpdateOrchestrationService, UpdateOrchestrationService>();

        services.AddTransient<IIgnoreService, IgnoreService>();
        services.AddTransient<IMaintenanceService, MaintenanceService>();
        services.AddTransient<IDiagnosticService, DiagnosticService>();
        services.AddTransient<IMisplacedModAnalyzerService, MisplacedModAnalyzerService>();
        services.AddTransient<IModLinkResolverService, ModLinkResolverService>();

        services.AddSingleton<ICacheManager, CacheManager>();
        services.AddSingleton<IScanCacheService, ScanCacheService>();
        services.AddSingleton<IIgnoredUpdateStore, IgnoredUpdateStore>();
        services.AddSingleton<IPluginScanCache, PluginScanCache>();

        // Parsers
        services.AddTransient<IBinaryParser, BinaryParser>();
        services.AddTransient<IJsonManifestParser, JsonManifestParser>();

        services.AddSingleton<IModCheckReporter, SpectreModCheckReporter>();
        services.AddSingleton<IDependencyUiRenderer, DependencyUiRenderer>();
        services.AddSingleton<IMisplacedModUiRenderer, MisplacedModUiRenderer>();
        services.AddSingleton<IProgressRenderer, ProgressRenderer>();
        services.AddSingleton<IReconciliationUiRenderer, ReconciliationUiRenderer>();

        services.AddSingleton<ITextRenderer, TextRenderer>();
        services.AddSingleton<IVersionTableUiRenderer, VersionTableUiRenderer>();
        services.AddSingleton<IMiscTableUiRenderer, MiscTableUiRenderer>();
        services.AddSingleton<IUserPromptService, InteractivePromptService>();

        services.AddTransient<IForgeApiClient, ForgeApiClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<System.Net.Http.IHttpClientFactory>().CreateClient("ForgeApi");
            return ActivatorUtilities.CreateInstance<ForgeApiClient>(sp, httpClient);
        });

        services.AddTransient<ISptVersionClient, CachedSptVersionClient>(sp =>
        {
            var inner = ActivatorUtilities.CreateInstance<SptVersionClient>(sp);
            return new CachedSptVersionClient(
                inner,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedSptVersionClient>>()
            );
        });

        services.AddTransient<IModSearchClient, CachedModSearchClient>(sp =>
        {
            var inner = ActivatorUtilities.CreateInstance<ModSearchClient>(sp);
            return new CachedModSearchClient(
                inner,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedModSearchClient>>()
            );
        });

        services.AddTransient<IModUpdateClient, CachedModUpdateClient>(sp =>
        {
            var inner = ActivatorUtilities.CreateInstance<ModUpdateClient>(sp);
            return new CachedModUpdateClient(
                inner,
                sp.GetRequiredService<IMemoryCache>(),
                sp.GetRequiredService<ILogger<CachedModUpdateClient>>()
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
        services.AddTransient<IWorkflowStep, CacheResultsStep>();
        services.AddTransient<IWorkflowStep, DisplayResultsStep>();

                var handler = new SPTarkov.DI.DependencyInjectionHandler(services);
        handler.AddInjectableTypesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        handler.InjectAll();
        return services;
    }
}
