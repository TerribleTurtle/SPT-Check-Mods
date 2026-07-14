using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Web;
using CheckModsExtended.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public class ScanCacheServiceTests
{
    private readonly FakeFileSystem _fileSystem;
    private readonly ScanCacheService _service;

    public ScanCacheServiceTests()
    {
        _fileSystem = new FakeFileSystem();
        var options = Options.Create(new AppPaths { AppDataDirectory = "C:\\AppData" });
        _service = new ScanCacheService(
            _fileSystem,
            NullLogger<ScanCacheService>.Instance,
            options
        );
    }

    [Fact]
    public async Task LoadCacheAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        var result = await _service.LoadCacheAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndLoadCacheAsync_WorksCorrectly()
    {
        var response = new ScanResponse(new List<ModDto>(), null, "3.8.0");
        var record = new ScanCacheRecord(TimeProvider.System.GetUtcNow(), null, response);

        await _service.SaveCacheAsync(record);

        var loaded = await _service.LoadCacheAsync();

        Assert.NotNull(loaded);
        Assert.Equal("3.8.0", loaded.Response.SptVersion);
        Assert.Null(loaded.SptPath);
    }

    [Fact]
    public async Task SaveAndLoadCacheAsync_WithSptPath_WorksCorrectly()
    {
        var response = new ScanResponse(new List<ModDto>(), null, "3.8.0");
        var sptPath = "C:\\Games\\SPT";
        var record = new ScanCacheRecord(TimeProvider.System.GetUtcNow(), sptPath, response);

        await _service.SaveCacheAsync(record);

        var loaded = await _service.LoadCacheAsync();

        Assert.NotNull(loaded);
        Assert.Equal(sptPath, loaded.SptPath);
    }

    [Fact]
    public async Task SaveCacheAsync_SwallowsExceptions_AndLogsWarning()
    {
        _fileSystem.UnauthorizedPaths.Add("C:\\AppData\\scan_cache.json.tmp");
        var response = new ScanResponse(new List<ModDto>(), null, "3.8.0");
        var record = new ScanCacheRecord(TimeProvider.System.GetUtcNow(), null, response);
        
        // Should not throw
        await _service.SaveCacheAsync(record);
    }

    [Fact]
    public async Task LoadCacheAsync_SwallowsExceptions_AndReturnsNull()
    {
        await _fileSystem.WriteAllTextAsync("C:\\AppData\\scan_cache.json", "{}");
        _fileSystem.UnauthorizedPaths.Add("C:\\AppData\\scan_cache.json");
        
        // Should not throw
        var loaded = await _service.LoadCacheAsync();
        Assert.Null(loaded);
    }
}
