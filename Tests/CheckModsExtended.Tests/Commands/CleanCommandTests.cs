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
        var result = await command.ExecuteInternalAsync(null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        Assert.False(Directory.Exists(testDir));
    }
}
