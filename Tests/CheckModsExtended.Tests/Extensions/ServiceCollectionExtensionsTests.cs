using CheckModsExtended.Extensions;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheckModsExtended.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Resolves_forge_api_service_successfully_with_resilience()
    {
        // Arrange
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "ForgeApiOptions:BaseUrl", "https://api.example.com/" },
                    { "LoggingOptions:EnableFileLogging", "false" },
                    { "LoggingOptions:MinimumLogLevel", "Information" },
                    { "IgnoredUpdateOptions:RemoteTimeoutSeconds", "10" },
                }
            )
            .Build();

        // Act
        services.AddCheckModsExtendedServices(configuration, new CheckModsExtended.Configuration.RuntimeConfig());
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var sptVersionClient = serviceProvider.GetRequiredService<ISptVersionClient>();
        var modSearchClient = serviceProvider.GetRequiredService<IModSearchClient>();
        var modUpdateClient = serviceProvider.GetRequiredService<IModUpdateClient>();
        
        Assert.NotNull(sptVersionClient);
        Assert.NotNull(modSearchClient);
        Assert.NotNull(modUpdateClient);
    }
}
