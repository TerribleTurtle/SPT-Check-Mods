using System.IO;
using CheckModsExtended.Configuration;
using CheckModsExtended.Extensions;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace CheckModsExtended.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void DependencyInjection_resolves_all_critical_services()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        var runtimeConfig = new RuntimeConfig
        {
            IsHeadless = true,
            IsVerbose = false,
            IsDebug = false
        };

        var services = new ServiceCollection();
        
        // Emulate what WebManagerHost does to configure services
        services.AddCheckModsExtendedServices(configuration, runtimeConfig);
        services.AddSingleton(runtimeConfig);

        // WebManagerHost adds logging, which services might depend on
        services.AddLogging();

        using var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - Ensure we can resolve critical orchestration and root services
        // If any dependencies are missing (e.g. IScanCacheService is required by IModScannerService),
        // GetRequiredService will throw an InvalidOperationException.
        
        var orchestrator = serviceProvider.GetRequiredService<IUpdateWorkflowOrchestrator>();
        Assert.NotNull(orchestrator);

        var scanner = serviceProvider.GetRequiredService<IModScannerService>();
        Assert.NotNull(scanner);

        var settingsService = serviceProvider.GetRequiredService<ISettingsService>();
        Assert.NotNull(settingsService);

        var cacheService = serviceProvider.GetRequiredService<IScanCacheService>();
        Assert.NotNull(cacheService);
        
        var pluginCache = serviceProvider.GetRequiredService<IPluginScanCache>();
        Assert.NotNull(pluginCache);
    }
}
