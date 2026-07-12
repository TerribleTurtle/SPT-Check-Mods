using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Configuration;
using Microsoft.Extensions.Options;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class DiagCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenLogsDirectoryDoesNotExist_ReturnsZero()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "CheckModsExtended_DiagCommandTest");
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, true);
        }

        var appPaths = new AppPaths { AppDataDirectory = testDir };
        var options = Options.Create(appPaths);

        var command = new DiagCommand(options);
        var settings = new GlobalSettings();
        // Act
        var method = typeof(DiagCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Equal(0, result);
    }
}
