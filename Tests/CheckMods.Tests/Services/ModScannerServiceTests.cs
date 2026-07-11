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

public sealed class ModScannerServiceTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly ModScannerService _service;
    private readonly FakeModCheckReporter _reporter;
    private readonly FakePluginMetadataExtractor _pluginExtractor;
    private readonly FakeServerModExtractor _serverExtractor;
    private readonly FakeMisplacedModDetector _misplacedDetector;
    private readonly string _sptPath;

    public ModScannerServiceTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;
        _reporter = new FakeModCheckReporter();
        
        _pluginExtractor = new FakePluginMetadataExtractor();
        _serverExtractor = new FakeServerModExtractor();
        _misplacedDetector = new FakeMisplacedModDetector();

        _service = new ModScannerService(
            _pluginExtractor,
            _serverExtractor,
            _misplacedDetector,
            _reporter,
            NullLogger<ModScannerService>.Instance
        );
    }

    [Fact]
    public void ScanServerMods_ReturnsValidMods()
    {
        var modPath = Path.Combine(_sptPath, "SPT", "user", "mods", "test-server-mod");
        Directory.CreateDirectory(modPath);
        File.WriteAllText(Path.Combine(modPath, "TestMod.dll"), "dummy");

        var fakeMod = new Mod { Local = new CheckMods.Models.LocalModIdentity { Guid = "com.server.test", FilePath = "test", LocalName = "Test Server Mod", LocalAuthor = "ServerAuthor", LocalVersion = "1.0.0", LocalSptVersion = "3.8.0", IsServerMod = true } };
        _serverExtractor.ExtractedMod = fakeMod;

        var mods = _service.ScanServerMods(_sptPath);

        Assert.Single(mods);
        Assert.Same(fakeMod, mods[0]);
    }

    [Fact]
    public async Task ScanClientModsAsync_ReturnsValidMods()
    {
        var pluginsDir = Path.Combine(_sptPath, "BepInEx", "plugins");
        Directory.CreateDirectory(pluginsDir);

        var dllPath = Path.Combine(pluginsDir, "TestClient.dll");
        File.WriteAllText(dllPath, "dummy");

        _pluginExtractor.ValidClientDllFilesToReturn = [dllPath];

        var fakeMod = new Mod { Local = new CheckMods.Models.LocalModIdentity { Guid = "com.client.test", FilePath = "test", LocalName = "Test Client Mod", LocalAuthor = "client", LocalVersion = "1.0.0", IsServerMod = false } };
        _pluginExtractor.ProcessedClientMods = [fakeMod];

        var mods = await _service.ScanClientModsAsync(_sptPath);

        Assert.Single(mods);
        Assert.Same(fakeMod, mods[0]);
    }

    [Fact]
    public async Task ScanAllModsAsync_WithMissingDirectories_ReturnsEmpty()
    {
        var (serverMods, clientMods) = await _service.ScanAllModsAsync(_sptPath);

        Assert.Empty(serverMods);
        Assert.Empty(clientMods);
    }

    [Fact]
    public void DetectMisplacedMods_CallsDetector()
    {
        var expectedReport = new MisplacedModReport([], []);
        _misplacedDetector.ReportToReturn = expectedReport;

        var report = _service.DetectMisplacedMods(_sptPath);

        Assert.Same(expectedReport, report);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    [Fact]
    public async Task E2e_scan_all_mods_async_returns_valid_mods()
    {
        // Setup real extractors
        var options = Microsoft.Extensions.Options.Options.Create(new CheckMods.Configuration.ModScannerOptions());
        var realPluginExtractor = new PluginMetadataExtractor(options, NullLogger<PluginMetadataExtractor>.Instance);
        var realServerExtractor = new ServerModExtractor(NullLogger<ServerModExtractor>.Instance);
        var realMisplacedDetector = new MisplacedModDetector(realPluginExtractor, realServerExtractor, NullLogger<MisplacedModDetector>.Instance);
        var realService = new ModScannerService(
            realPluginExtractor,
            realServerExtractor,
            realMisplacedDetector,
            _reporter,
            NullLogger<ModScannerService>.Instance
        );

        // Generate dummy client mod (BepInEx Plugin)
        var clientModCode = @"
using BepInEx;
[BepInPlugin(""com.client.test"", ""Test Client Mod"", ""1.0.0"")]
public class TestClientPlugin : BaseUnityPlugin {}
namespace BepInEx {
    public class BaseUnityPlugin {}
    public class BepInPlugin : System.Attribute {
        public BepInPlugin(string guid, string name, string version) {}
    }
}";
        _fixture.CompileDummyDll(Path.Combine("BepInEx", "plugins", "TestClient.dll"), clientModCode);

        // Generate dummy server mod
        var serverModCode = @"
public abstract class AbstractModMetadata {
    public string ModGuid { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string Version { get; set; }
    public string SptVersion { get; set; }
}
public class TestServerMod : AbstractModMetadata {
    public TestServerMod() {
        ModGuid = ""com.server.test"";
        Name = ""Test Server Mod"";
        Author = ""ServerAuthor"";
        Version = ""1.0.0"";
        SptVersion = ""3.8.0"";
    }
}";
        _fixture.CompileDummyDll(Path.Combine("SPT", "user", "mods", "test-server-mod", "TestMod.dll"), serverModCode);

        // Run the real service
        var (serverMods, clientMods) = await realService.ScanAllModsAsync(_sptPath);

        // Assert
        Assert.Single(clientMods);
        Assert.Equal("com.client.test", clientMods[0].Local.Guid);
        Assert.Equal("Test Client Mod", clientMods[0].Local.LocalName);
        
        Assert.Single(serverMods);
        Assert.Equal("com.server.test", serverMods[0].Local.Guid);
        Assert.Equal("Test Server Mod", serverMods[0].Local.LocalName);
    }
}






