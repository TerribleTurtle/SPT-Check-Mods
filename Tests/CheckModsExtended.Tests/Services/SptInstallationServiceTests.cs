using System;
using System.IO;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using CheckModsExtended.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class SptInstallationServiceTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly SptInstallationService _service;
    private readonly CheckModsExtended.Tests.Fakes.FakeModScannerService _scannerService;
    private readonly FakeForgeApiService _forgeApiService;
    private readonly CheckModsExtended.Tests.Fakes.FakeModCheckReporter _reporter;
    private readonly string _sptPath;

    public SptInstallationServiceTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;

        _scannerService = new CheckModsExtended.Tests.Fakes.FakeModScannerService();
        _forgeApiService = new FakeForgeApiService();
        _reporter = new CheckModsExtended.Tests.Fakes.FakeModCheckReporter();

        _service = new SptInstallationService(
            _forgeApiService,
            _scannerService,
            _reporter,
            NullLogger<SptInstallationService>.Instance
        );
    }

    [Fact]
    public async Task Getandvalidatesptversionasync_withvalidversion_returnsparsedversion()
    {
        var coreDllPath = Path.Combine(_sptPath, "SPT", "SPTarkov.Server.Core.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(coreDllPath)!);
        File.WriteAllText(coreDllPath, "dummy content");

        _scannerService.SptVersionToReturn = "3.8.0";
        _forgeApiService.OnValidateSptVersion = _ => true;

        var result = await _service.GetAndValidateSptVersionAsync(_sptPath);

        Assert.NotNull(result);
        Assert.Equal("3.8.0", result.ToString());
    }

    [Fact]
    public async Task Getandvalidatesptversionasync_missingcoredll_returnsnull()
    {
        var result = await _service.GetAndValidateSptVersionAsync(_sptPath);

        Assert.Null(result);
    }

    [Fact]
    public async Task Getandvalidatesptversionasync_invalidversion_returnsnull()
    {
        var coreDllPath = Path.Combine(_sptPath, "SPT", "SPTarkov.Server.Core.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(coreDllPath)!);
        File.WriteAllText(coreDllPath, "dummy content");

        _scannerService.SptVersionToReturn = "3.8.0";
        _forgeApiService.OnValidateSptVersion = _ => false;

        var result = await _service.GetAndValidateSptVersionAsync(_sptPath);

        Assert.Null(result);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

