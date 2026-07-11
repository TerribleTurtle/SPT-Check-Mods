using CheckMods.Extensions;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheckMods.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Resolves_forge_api_service_successfully_with_resilience()
    {
        // Arrange
        var services = new ServiceCollection();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ForgeApiOptions:BaseUrl", "https://api.example.com/" },
                { "RateLimitOptions:RequestTimeoutSeconds", "30" },
                { "LoggingOptions:EnableFileLogging", "false" },
                { "LoggingOptions:MinimumLogLevel", "Information" },
                { "IgnoredUpdateOptions:RemoteTimeoutSeconds", "10" }
            })
            .Build();

        // Act
        services.AddCheckModsServices(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var forgeApiService = serviceProvider.GetRequiredService<IForgeApiService>();
        Assert.NotNull(forgeApiService);
    }
}
