using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class DiagCommandTests
{
    private sealed class FakeDiagnosticService : IDiagnosticService
    {
        public bool WasExportCalled { get; private set; }
        public Task<string?> ExportLogsAsync(CancellationToken cancellationToken = default)
        {
            WasExportCalled = true;
            return Task.FromResult<string?>("dummy.zip");
        }
    }

    [Fact]
    public async Task ExecuteAsync_CallsExportLogsAsync()
    {
        // Arrange
        var fakeService = new FakeDiagnosticService();
        var command = new DiagCommand(fakeService);
        var settings = new GlobalSettings();
        
        // Act
        var result = await command.ExecuteInternalAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.True(fakeService.WasExportCalled);
    }
}
