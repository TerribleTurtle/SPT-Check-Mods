using System;
using System.IO;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services;
using CheckMods.Tests.Fakes;
using CheckMods.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckMods.Tests.Services;

public sealed class SptInstallationServiceTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly SptInstallationService _service;
    private readonly CheckMods.Tests.Fakes.FakeModScannerService _scannerService;
    private readonly CheckMods.Tests.Fakes.FakeForgeApiService _forgeApiService;
    private readonly CheckMods.Tests.Fakes.FakeModCheckReporter _reporter;
    private readonly string _sptPath;

    public SptInstallationServiceTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;

        _scannerService = new CheckMods.Tests.Fakes.FakeModScannerService();
        _forgeApiService = new CheckMods.Tests.Fakes.FakeForgeApiService();
        _reporter = new CheckMods.Tests.Fakes.FakeModCheckReporter();

        _service = new SptInstallationService(
            _forgeApiService,
            _scannerService,
            _reporter,
            NullLogger<SptInstallationService>.Instance
        );
    }

    [Fact]
    public async Task GetAndValidateSptVersionAsync_WithValidVersion_ReturnsParsedVersion()
    {
        var coreDllPath = Path.Combine(_sptPath, "SPT", "SPTarkov.Server.Core.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(coreDllPath)!);
        File.WriteAllText(coreDllPath, "dummy content");

        _scannerService.SptVersionToReturn = "3.8.0";
        _forgeApiService.ValidateSptVersionResult = true;

        var result = await _service.GetAndValidateSptVersionAsync(_sptPath);

        Assert.NotNull(result);
        Assert.Equal("3.8.0", result.ToString());
    }

    [Fact]
    public async Task GetAndValidateSptVersionAsync_MissingCoreDll_ReturnsNull()
    {
        var result = await _service.GetAndValidateSptVersionAsync(_sptPath);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAndValidateSptVersionAsync_InvalidVersion_ReturnsNull()
    {
        var coreDllPath = Path.Combine(_sptPath, "SPT", "SPTarkov.Server.Core.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(coreDllPath)!);
        File.WriteAllText(coreDllPath, "dummy content");

        _scannerService.SptVersionToReturn = "3.8.0";
        _forgeApiService.ValidateSptVersionResult = new InvalidSptVersion();

        var result = await _service.GetAndValidateSptVersionAsync(_sptPath);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}






