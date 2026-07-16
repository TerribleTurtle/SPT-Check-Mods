using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using CheckModsExtended.Utils;
using Spectre.Console.Testing;

namespace CheckModsExtended.Tests.Commands;

public class IgnoreRemoveCommandTests
{
    private sealed class FakeIgnoreService : IIgnoreService
    {
        public bool RemoveCalled { get; private set; }
        public int ReturnValue { get; set; } = 1;
        
        public Task<bool> AddIgnoreAsync(int apiModId, string localVersion, string latestVersion, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<IgnoredUpdate>> GetIgnoresAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<IgnoredUpdate>>([]);

        public Task<int> RemoveIgnoreAsync(int apiModId, CancellationToken cancellationToken = default)
        {
            RemoveCalled = true;
            return Task.FromResult(ReturnValue);
        }
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesCorrectly()
    {
        var fakeService = new FakeIgnoreService { ReturnValue = 1 };
        var fakeReporter = new FakeModCheckReporter();
        
        var services = new ServiceCollection();
        services.AddSingleton<IIgnoreService>(fakeService);
        services.AddSingleton<IModCheckReporter>(fakeReporter);
        var app = new CommandApp<IgnoreRemoveCommand>(new TypeRegistrar(services));
        app.Configure(config => config.ConfigureConsole(new TestConsole()));
        var exitCode = await app.RunAsync(new[] { "1" });

        Assert.Equal(0, exitCode);
        Assert.True(fakeService.RemoveCalled);
    }
}
