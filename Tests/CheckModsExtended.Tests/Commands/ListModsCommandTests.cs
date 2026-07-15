using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CheckModsExtended.Utils;
using Spectre.Console.Testing;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Commands;

public class ListModsCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPathValid_ExecutesCorrectly()
    {
        var initService = new FakeInitializationService { ValidatedSptPathToReturn = "C:\\SPT" };
        var scannerService = new FakeModScannerService();
        var reporter = new FakeModCheckReporter();
        
        var services = new ServiceCollection();
        services.AddSingleton<IInitializationService>(initService);
        services.AddSingleton<IModScannerService>(scannerService);
        services.AddSingleton<IModCheckReporter>(reporter);
        var app = new CommandApp<ListModsCommand>(new TypeRegistrar(services));
        app.Configure(config => config.ConfigureConsole(new TestConsole()));
        var exitCode = await app.RunAsync(new string[0]);

        Assert.Equal(0, exitCode);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenPathInvalid_ReturnsError()
    {
        var initService = new FakeInitializationService { ValidatedSptPathToReturn = null };
        var scannerService = new FakeModScannerService();
        var reporter = new FakeModCheckReporter();
        
        var services = new ServiceCollection();
        services.AddSingleton<IInitializationService>(initService);
        services.AddSingleton<IModScannerService>(scannerService);
        services.AddSingleton<IModCheckReporter>(reporter);
        var app = new CommandApp<ListModsCommand>(new TypeRegistrar(services));
        app.Configure(config => config.ConfigureConsole(new TestConsole()));
        var exitCode = await app.RunAsync(new[] { "invalid" });

        Assert.Equal(1, exitCode);
    }
}
