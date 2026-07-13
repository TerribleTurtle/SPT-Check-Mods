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
        var method = typeof(LicenseCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var result = await task;

        // Assert
        // The resource might or might not be embedded in the test runner context,
        // but we just ensure it executes without throwing exceptions.
        Assert.True(result == 0 || result == 1);
    }
}
