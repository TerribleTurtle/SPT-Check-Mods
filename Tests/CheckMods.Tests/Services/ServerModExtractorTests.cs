using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CheckMods.Services;
using CheckMods.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckMods.Tests.Services;

public sealed class ServerModExtractorTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly ServerModExtractor _extractor;
    private readonly string _sptPath;

    public ServerModExtractorTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;
        _extractor = new ServerModExtractor(NullLogger<ServerModExtractor>.Instance);
    }

    [Fact]
    public async Task extractservermodmetadataasync_returnsvalidmod()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "test-server-mod", "TestServerMod.dll");

        var code = @"
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
    public async Task extractservermodmetadataasync_returnsnull_formissingguid()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "missing-props-mod", "MissingProps.dll");

        var code = @"
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
    public async Task extractservermodmetadataasync_returnsmodwithwarnings_formissingproperties()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "missing-props-mod", "MissingProps.dll");

        var code = @"
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
    public async Task extractservermodmetadataasync_returnsnull_forunreadablefile()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "unreadable-mod");
        var dllPath = Path.Combine(modPath, "Unreadable.dll");

        Directory.CreateDirectory(Path.Combine(_sptPath, modPath));
        File.WriteAllBytes(Path.Combine(_sptPath, dllPath), [0x00, 0x01, 0x02]);

        var mod = await _extractor.ExtractServerModMetadataAsync(Path.Combine(_sptPath, dllPath), _sptPath);

        Assert.Null(mod);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
