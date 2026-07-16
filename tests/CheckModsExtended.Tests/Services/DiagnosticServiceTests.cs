using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public class DiagnosticServiceTests : IDisposable
{
    private readonly string _testBaseDir;
    
    public DiagnosticServiceTests()
    {
        _testBaseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testBaseDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testBaseDir))
        {
            Directory.Delete(_testBaseDir, true);
        }
    }

    [Fact]
    public async Task ExportLogsAsync_NoLogsDir_ReturnsNull()
    {
        var appPaths = new AppPaths { AppDataDirectory = _testBaseDir };
        var options = Options.Create(appPaths);
        var service = new DiagnosticService(options);

        var result = await service.ExportLogsAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ExportLogsAsync_WithLogsDir_ReturnsZipPath()
    {
        var appPaths = new AppPaths { AppDataDirectory = _testBaseDir };
        var logsDir = Path.Combine(_testBaseDir, "logs");
        Directory.CreateDirectory(logsDir);
        File.WriteAllText(Path.Combine(logsDir, "test-log.txt"), "fake log data");

        var options = Options.Create(appPaths);
        var service = new DiagnosticService(options);

        var result = await service.ExportLogsAsync();

        Assert.NotNull(result);
        Assert.True(File.Exists(result));

        // Cleanup the created zip file in current directory
        if (File.Exists(result))
        {
            File.Delete(result);
        }
    }
}
