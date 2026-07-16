using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CheckModsExtended.Utils;
using Spectre.Console.Testing;

namespace CheckModsExtended.Tests.Commands;

public class IgnoreAddCommandTests
{
    private sealed class FakeIgnoreService : IIgnoreService
    {
        public bool AddCalled { get; private set; }
        public bool ReturnValue { get; set; } = true;
        
        public Task<bool> AddIgnoreAsync(int apiModId, string localVersion, string latestVersion, CancellationToken cancellationToken = default)
        {
            AddCalled = true;
            return Task.FromResult(ReturnValue);
        }

        public Task<System.Collections.Generic.IReadOnlyList<CheckModsExtended.Models.IgnoredUpdate>> GetIgnoresAsync(CancellationToken cancellationToken = default) => throw new System.NotImplementedException();
        public Task<int> RemoveIgnoreAsync(int apiModId, CancellationToken cancellationToken = default) => throw new System.NotImplementedException();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdded_ExecutesCorrectly()
    {
        var fakeService = new FakeIgnoreService { ReturnValue = true };
        var fakeReporter = new FakeModCheckReporter();
        
        var services = new ServiceCollection();
        services.AddSingleton<IIgnoreService>(fakeService);
        services.AddSingleton<IModCheckReporter>(fakeReporter);
        var app = new CommandApp<IgnoreAddCommand>(new TypeRegistrar(services));
        app.Configure(config => config.ConfigureConsole(new TestConsole()));
        var exitCode = await app.RunAsync(new[] { "1", "1.0", "2.0" });

        Assert.Equal(0, exitCode);
        Assert.True(fakeService.AddCalled);
    }
}
