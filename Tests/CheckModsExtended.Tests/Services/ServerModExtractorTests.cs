using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using CheckModsExtended.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class ServerModExtractorTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly ServerModExtractor _extractor;
    private readonly string _sptPath;

    public ServerModExtractorTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;
        _extractor = new ServerModExtractor(
            NullLogger<ServerModExtractor>.Instance, new FakeModCheckReporter(), new BinaryParser(_fixture.FileSystem), new JsonManifestParser(_fixture.FileSystem), _fixture.FileSystem
        );
    }

    [Fact]
    public async Task Extractservermodmetadataasync_returnsvalidmod()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "test-server-mod", "TestServerMod.dll");

        var code =
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

        var dllPath = _fixture.CompileDummyDll(modPath, code);

        var mod = await _extractor.ExtractServerModMetadataAsync(dllPath, _sptPath);

        Assert.NotNull(mod);
        Assert.True(mod!.Local.IsServerMod);
        Assert.Equal("ServerAuthor-Test Server Mod", mod.Local.Guid);
        Assert.Equal("Test Server Mod", mod.Local.LocalName);
        Assert.Equal("ServerAuthor", mod.Local.LocalAuthor);
        Assert.Equal("1.0.0", mod.Local.LocalVersion);
        Assert.Equal("3.8.0", mod.Local.LocalSptVersion);
    }

    [Fact]
    public async Task ExtractServerModPackageMetadataAsync_returns_valid_mod()
    {
        var modDir = Path.Combine(_sptPath, "SPT", "user", "mods", "PackageOnlyMod");
        _fixture.FileSystem.CreateDirectory(modDir);
        var packagePath = Path.Combine(modDir, "package.json");
        await _fixture.FileSystem.WriteAllTextAsync(
            packagePath,
            """
            {
              "name": "PackageOnlyMod",
              "author": "CheckMods",
              "version": "2.3.4",
              "sptVersion": "~4.0",
              "main": "src/mod.js"
            }
            """
        );

        var mod = await _extractor.ExtractServerModPackageMetadataAsync(modDir);

        Assert.NotNull(mod);
        Assert.True(mod!.Local.IsServerMod);
        Assert.Equal("PackageOnlyMod", mod.Local.Guid);
        Assert.Equal("PackageOnlyMod", mod.Local.LocalName);
        Assert.Equal("CheckMods", mod.Local.LocalAuthor);
        Assert.Equal("2.3.4", mod.Local.LocalVersion);
        Assert.Equal("~4.0", mod.Local.LocalSptVersion);
        Assert.Equal(packagePath, mod.Local.FilePath);
    }

    [Fact]
    public async Task Extractservermodmetadataasync_returnsnull_formissingguid()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "missing-props-mod", "MissingProps.dll");

        var code =
            @"
            using System;
            
            public abstract class AbstractModMetadata { }

            public class InvalidModMetadata : AbstractModMetadata 
            {
                public string Name { get; } = ""Test Server Mod"";
            }
        ";

        var dllPath = _fixture.CompileDummyDll(modPath, code);

        var mod = await _extractor.ExtractServerModMetadataAsync(dllPath, _sptPath);

        Assert.Null(mod);
    }

    [Fact]
    public async Task Extractservermodmetadataasync_returnsmodwithwarnings_formissingproperties()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "missing-props-mod", "MissingProps.dll");

        var code =
            @"
            using System;
            
            public abstract class AbstractModMetadata { }

            public class InvalidModMetadata : AbstractModMetadata 
            {
                public string ModGuid { get; } = ""ServerAuthor-Test Server Mod"";
            }
        ";

        var dllPath = _fixture.CompileDummyDll(modPath, code);

        var mod = await _extractor.ExtractServerModMetadataAsync(dllPath, _sptPath);

        Assert.NotNull(mod);
        Assert.True(mod!.HasWarnings);
        Assert.Contains(mod.LoadWarnings, w => w.Contains("Missing author"));
        Assert.Contains(mod.LoadWarnings, w => w.Contains("Missing mod name"));
    }

    [Fact]
    public async Task Extractservermodmetadataasync_returnsnull_forunreadablefile()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "unreadable-mod");
        var dllPath = Path.Combine(modPath, "Unreadable.dll");

        _fixture.FileSystem.CreateDirectory(Path.Combine(_sptPath, modPath));
        await _fixture.FileSystem.WriteAllTextAsync(Path.Combine(_sptPath, dllPath), "dummy");

        var mod = await _extractor.ExtractServerModMetadataAsync(Path.Combine(_sptPath, dllPath), _sptPath);

        Assert.Null(mod);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

