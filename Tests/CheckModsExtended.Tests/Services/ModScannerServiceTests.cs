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
            new FakePluginScanCache(),
            _pluginExtractor,
            _serverExtractor,
            _misplacedDetector,
            _reporter,
            NullLogger<ModScannerService>.Instance,
            _fixture.FileSystem
        );
    }

    [Fact]
    public async Task Scanservermodsasync_returnsvalidmods()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "test-server-mod", "TestServerMod.dll");
        var serverCode =
            @"
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

        var fakeMod = ModFixture.CreateServerMod(
            "ServerAuthor-Test Server Mod",
            "Test Server Mod",
            "1.0.0",
            "ServerAuthor"
        );
        fakeMod = fakeMod with { Local = fakeMod.Local with { FilePath = "test", LocalSptVersion = "3.8.0" } };
        _serverExtractor.ExtractedMod = fakeMod;

        var mods = await _service.ScanServerModsAsync(_sptPath);

        Assert.Single(mods);
        Assert.Same(fakeMod, mods[0]);
    }

    [Fact]
    public async Task Scanservermodsasync_includes_package_only_server_mods()
    {
        var modDir = Path.Combine(_sptPath, "SPT", "user", "mods", "PackageOnlyMod");
        _fixture.FileSystem.CreateDirectory(modDir);
        await _fixture.FileSystem.WriteAllTextAsync(Path.Combine(modDir, "package.json"), "{}");

        var fakeMod = ModFixture.CreateServerMod("PackageOnlyMod", "PackageOnlyMod", "1.0.0", "Unknown");
        fakeMod = fakeMod with { Local = fakeMod.Local with { FilePath = "test.json", LocalSptVersion = "3.8.0" } };
        // FakeServerModExtractor returns this mod for BOTH metadata extraction methods.
        // Because there are no DLLs in the directory, it will skip the DLL loop and hit the package fallback.
        _serverExtractor.ExtractedMod = fakeMod;

        var mods = await _service.ScanServerModsAsync(_sptPath);

        Assert.Single(mods);
        Assert.Same(fakeMod, mods[0]);
    }

    [Fact]
    public async Task Scanclientmodsasync_returnsvalidmods()
    {
        var pluginsDir = Path.Combine(_sptPath, "BepInEx", "plugins");
        _fixture.FileSystem.CreateDirectory(pluginsDir);

        var dllPath = Path.Combine(pluginsDir, "TestClient.dll");
        await _fixture.FileSystem.WriteAllTextAsync(dllPath, "dummy");

        _pluginExtractor.ValidClientDllFilesToReturn = [dllPath];

        var fakeMod = ModFixture.CreateClientMod("com.client.test", "Test Client Mod", "1.0.0", "client");
        fakeMod = fakeMod with { Local = fakeMod.Local with { FilePath = "test" } };
        _pluginExtractor.ProcessedClientMods = [fakeMod];

        var mods = await _service.ScanClientModsAsync(_sptPath);

        Assert.Single(mods);
        Assert.Same(fakeMod, mods[0]);
    }

    [Fact]
    public async Task Scanallmodsasync_withmissingdirectories_returnsempty()
    {
        var (serverMods, clientMods) = await _service.ScanAllModsAsync(_sptPath);

        Assert.Empty(serverMods);
        Assert.Empty(clientMods);
    }

    [Fact]
    public async Task Detectmisplacedmodsasync_callsdetector()
    {
        var expectedReport = new MisplacedModReport([], []);
        _misplacedDetector.ReportToReturn = expectedReport;

        var report = await _service.DetectMisplacedModsAsync(_sptPath);

        Assert.Same(expectedReport, report);
    }


    [Fact]
    public async Task ScanServerModsAsync_ThrowsUnauthorizedAccess_LogsWarningAndContinues()
    {
        var modDir = Path.Combine(_sptPath, "SPT", "user", "mods", "TestMod");
        _fixture.FileSystem.CreateDirectory(modDir);
        var dllPath = Path.Combine(modDir, "TestMod.dll");
        await _fixture.FileSystem.WriteAllTextAsync(dllPath, "dummy");

        _serverExtractor.ThrowUnauthorizedAccess = true;

        var mods = await _service.ScanServerModsAsync(_sptPath);

        Assert.Empty(mods);
        Assert.Contains(_reporter.Warnings, w => w.Contains("CouldNotReadModDll"));
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    [Fact]
    public async Task E2e_scan_all_mods_async_returns_valid_mods()
    {
        // Setup real extractors
        var options = Microsoft.Extensions.Options.Options.Create(
            new CheckModsExtended.Configuration.ModScannerOptions()
        );
        var realPluginExtractor = new PluginMetadataExtractor(
            new ModPartitioner(),
            options,
            NullLogger<PluginMetadataExtractor>.Instance,
            _reporter,
            _fixture.FileSystem
        );
        var realServerExtractor = new ServerModExtractor(
            NullLogger<ServerModExtractor>.Instance,
            _fixture.FileSystem,
            _reporter
        );
        var realMisplacedDetector = new MisplacedModDetector(
            new FakePluginScanCache(),
            new ModPartitioner(),
            realPluginExtractor,
            realServerExtractor,
            NullLogger<MisplacedModDetector>.Instance,
            _fixture.FileSystem
        );
        var realService = new ModScannerService(
            new FakePluginScanCache(),
            realPluginExtractor,
            realServerExtractor,
            realMisplacedDetector,
            _reporter,
            NullLogger<ModScannerService>.Instance,
            _fixture.FileSystem
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
        var serverCode =
            @"
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

