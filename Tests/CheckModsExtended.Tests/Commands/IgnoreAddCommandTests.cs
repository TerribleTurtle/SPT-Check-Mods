using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;

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
        var command = new IgnoreAddCommand(fakeService, fakeReporter);
        
        var settings = new IgnoreAddCommand.Settings { ApiModId = 1, LocalVersion = "1.0", LatestVersion = "2.0" };
        var method = typeof(IgnoreAddCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var result = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var exitCode = await result;

        Assert.Equal(0, exitCode);
        Assert.True(fakeService.AddCalled);
    }
}
