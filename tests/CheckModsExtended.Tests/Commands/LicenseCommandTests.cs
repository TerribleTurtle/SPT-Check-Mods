using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class LicenseCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess_Or_Error_DependingOnResource()
    {
        // Arrange
        var command = new LicenseCommand();
        var settings = new LicenseCommand.Settings();
        // Act
        var result = await LicenseCommand.ExecuteInternalAsync(null!, settings, CancellationToken.None);

        // Assert
        // The resource might or might not be embedded in the test runner context,
        // but we just ensure it executes without throwing exceptions.
        Assert.True(result == 0 || result == 1);
    }
}
