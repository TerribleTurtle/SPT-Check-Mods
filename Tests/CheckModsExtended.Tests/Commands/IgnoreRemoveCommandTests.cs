using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;

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
        var command = new IgnoreRemoveCommand(fakeService, fakeReporter);
        
        var settings = new IgnoreRemoveCommand.Settings { ApiModId = 1 };
        var method = typeof(IgnoreRemoveCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var result = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var exitCode = await result;

        Assert.Equal(0, exitCode);
        Assert.True(fakeService.RemoveCalled);
    }
}
