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
        var record = new ScanCacheRecord(TimeProvider.System.GetUtcNow(), response);

        await _service.SaveCacheAsync(record);
        
        var loaded = await _service.LoadCacheAsync();
        
        Assert.NotNull(loaded);
        Assert.Equal("3.8.0", loaded.Response.SptVersion);
    }
}
