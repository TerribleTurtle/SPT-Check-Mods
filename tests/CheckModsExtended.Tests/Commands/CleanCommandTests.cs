using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class CleanCommandTests
{
    private sealed class FakeMaintenanceService : IMaintenanceService
    {
        public bool WasCleanCalled { get; private set; }
        public Task<bool> CleanAppDataAsync(CancellationToken cancellationToken = default)
        {
            WasCleanCalled = true;
            return Task.FromResult(true);
        }
    }

    [Fact]
    public async Task ExecuteAsync_CallsCleanAppDataAsync()
    {
        // Arrange
        var fakeService = new FakeMaintenanceService();
        var command = new CleanCommand(fakeService);
        var settings = new GlobalSettings();
        
        // Act
        var result = await command.ExecuteInternalAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.True(fakeService.WasCleanCalled);
    }
}
