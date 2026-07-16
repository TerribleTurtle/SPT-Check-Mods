using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CheckModsExtended.Utils;
using Spectre.Console.Testing;

namespace CheckModsExtended.Tests.Commands;

public class IgnoreListCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ExecutesCorrectly()
    {
        var store = new FakeIgnoredUpdateStore();
        var reporter = new FakeModCheckReporter();
        
        var services = new ServiceCollection();
        services.AddSingleton<CheckModsExtended.Services.Interfaces.IIgnoredUpdateStore>(store);
        services.AddSingleton<CheckModsExtended.Services.Interfaces.IModCheckReporter>(reporter);
        var app = new CommandApp<IgnoreListCommand>(new TypeRegistrar(services));
        app.Configure(config => config.ConfigureConsole(new TestConsole()));
        var exitCode = await app.RunAsync(new string[0]);

        Assert.Equal(0, exitCode);
    }
}
