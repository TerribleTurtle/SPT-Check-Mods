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
    public async Task scanservermodsasync_returnsvalidmods()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "test-server-mod", "TestServerMod.dll");
        var serverCode = @"
            using System;
            public abstract class AbstractModMetadata { }
            public class ValidModMetadata : AbstractModMetadata 
            {
                public string ModGuid { get; } = ""ServerAuthor-Test Server Mod"";
                public string Name { get; } = ""Test Server Mod"";
                public string Author { get; } = ""ServerAuthor"";
                public string Version { get; } = ""1.0.0"";
                public string SptVersion { get; } = ""3.8.0"";
            }
        ";
        _fixture.CompileDummyDll(modPath, serverCode);

        var fakeMod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
            {
                Guid = "ServerAuthor-Test Server Mod",
                FilePath = "test",
                LocalName = "Test Server Mod",
                LocalAuthor = "ServerAuthor",
                LocalVersion = "1.0.0",
                LocalSptVersion = "3.8.0",
                IsServerMod = true,
            },
        };
        _serverExtractor.ExtractedMod = fakeMod;

        var mods = await _service.ScanServerModsAsync(_sptPath);

        Assert.Single(mods);
        Assert.Same(fakeMod, mods[0]);
    }

    [Fact]
    public async Task scanclientmodsasync_returnsvalidmods()
    {
        var pluginsDir = Path.Combine(_sptPath, "BepInEx", "plugins");
        Directory.CreateDirectory(pluginsDir);

        var dllPath = Path.Combine(pluginsDir, "TestClient.dll");
        File.WriteAllText(dllPath, "dummy");

        _pluginExtractor.ValidClientDllFilesToReturn = [dllPath];

        var fakeMod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
            {
                Guid = "com.client.test",
                FilePath = "test",
                LocalName = "Test Client Mod",
                LocalAuthor = "client",
                LocalVersion = "1.0.0",
                IsServerMod = false,
            },
        };
        _pluginExtractor.ProcessedClientMods = [fakeMod];

        var mods = await _service.ScanClientModsAsync(_sptPath);

        Assert.Single(mods);
        Assert.Same(fakeMod, mods[0]);
    }

    [Fact]
    public async Task scanallmodsasync_withmissingdirectories_returnsempty()
    {
        var (serverMods, clientMods) = await _service.ScanAllModsAsync(_sptPath);

        Assert.Empty(serverMods);
        Assert.Empty(clientMods);
    }

    [Fact]
    public async Task detectmisplacedmodsasync_callsdetector()
    {
        var expectedReport = new MisplacedModReport([], []);
        _misplacedDetector.ReportToReturn = expectedReport;

        var report = await _service.DetectMisplacedModsAsync(_sptPath);

        Assert.Same(expectedReport, report);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    [Fact]
    public async Task e2e_scan_all_mods_async_returns_valid_mods()
    {
        // Setup real extractors
        var options = Microsoft.Extensions.Options.Options.Create(new CheckMods.Configuration.ModScannerOptions());
        var realPluginExtractor = new PluginMetadataExtractor(options, NullLogger<PluginMetadataExtractor>.Instance);
        var realServerExtractor = new ServerModExtractor(NullLogger<ServerModExtractor>.Instance);
        var realMisplacedDetector = new MisplacedModDetector(
            realPluginExtractor,
            realServerExtractor,
            NullLogger<MisplacedModDetector>.Instance
        );
        var realService = new ModScannerService(
            realPluginExtractor,
            realServerExtractor,
            realMisplacedDetector,
            _reporter,
            NullLogger<ModScannerService>.Instance
        );

        // Generate dummy client mod (BepInEx Plugin)
        var clientModCode =
            @"
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
        var modPath = Path.Combine("SPT", "user", "mods", "test-server-mod", "TestServerMod.dll");
        var serverCode = @"
            using System;
            public abstract class AbstractModMetadata { }
            public class ValidModMetadata : AbstractModMetadata 
            {
                public string ModGuid { get; } = ""ServerAuthor-Test Server Mod"";
                public string Name { get; } = ""Test Server Mod"";
                public string Author { get; } = ""ServerAuthor"";
                public string Version { get; } = ""1.0.0"";
                public string SptVersion { get; } = ""3.8.0"";
            }
        ";
        _fixture.CompileDummyDll(modPath, serverCode);

        // Run the real service
        var (serverMods, clientMods) = await realService.ScanAllModsAsync(_sptPath);

        // Assert
        Assert.Single(clientMods);
        Assert.Equal("com.client.test", clientMods[0].Local.Guid);
        Assert.Equal("Test Client Mod", clientMods[0].Local.LocalName);

        Assert.Single(serverMods);
        Assert.Equal("ServerAuthor-Test Server Mod", serverMods[0].Local.Guid);
        Assert.Equal("Test Server Mod", serverMods[0].Local.LocalName);
    }
}
