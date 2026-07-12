using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Configuration;
using Microsoft.Extensions.Options;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class CleanCommandTests
{
    [Fact]
    public async Task ExecuteAsync_CleansAppDataDirectory()
    {
        // Arrange
        var testDir = Path.Combine(Path.GetTempPath(), "CheckModsExtended_CleanCommandTest");
        if (!Directory.Exists(testDir))
        {
            Directory.CreateDirectory(testDir);
        }

        var appPaths = new AppPaths { AppDataDirectory = testDir };
        var options = Options.Create(appPaths);

        var command = new CleanCommand(options);
        var settings = new GlobalSettings();
        // Act
        var method = typeof(CleanCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var result = await task;

        // Assert
        Assert.Equal(0, result);
        Assert.False(Directory.Exists(testDir));
    }
}
