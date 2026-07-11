using System;
using System.IO;
using System.Linq;
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
    public void ExtractServerModMetadata_ReturnsValidMod()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "test-server-mod");
        var dllPath = Path.Combine(modPath, "TestMod.dll");

        var code = @"
using System;
public abstract class AbstractModMetadata 
{
    public string ModGuid { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string Version { get; set; }
    public string SptVersion { get; set; }
}

public class MySptMod : AbstractModMetadata 
{
    public MySptMod() 
    {
        ModGuid = ""com.server.test"";
        Name = ""Test Server Mod"";
        Author = ""ServerAuthor"";
        Version = ""1.0.0"";
        SptVersion = ""3.8.0"";
    }
}
";
        _fixture.CompileDummyDll(dllPath, code);

        var mod = _extractor.ExtractServerModMetadata(Path.Combine(_sptPath, dllPath), _sptPath);

        Assert.NotNull(mod);
        Assert.True(mod!.Local.IsServerMod);
        Assert.Equal("com.server.test", mod.Local.Guid);
        Assert.Equal("Test Server Mod", mod.Local.LocalName);
        Assert.Equal("ServerAuthor", mod.Local.LocalAuthor);
        Assert.Equal("1.0.0", mod.Local.LocalVersion);
        Assert.Equal("3.8.0", mod.Local.LocalSptVersion);
    }

    [Fact]
    public void ExtractServerModMetadata_ReturnsNull_ForMissingProperties()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "missing-props-mod");
        var dllPath = Path.Combine(modPath, "MissingProps.dll");

        var code = @"
public abstract class AbstractModMetadata 
{
    public string ModGuid { get; set; }
}

public class MySptMod : AbstractModMetadata 
{
    public MySptMod() 
    {
        ModGuid = """";
    }
}
";
        _fixture.CompileDummyDll(dllPath, code);

        var mod = _extractor.ExtractServerModMetadata(Path.Combine(_sptPath, dllPath), _sptPath);

        Assert.Null(mod);
    }

    [Fact]
    public void ExtractServerModMetadata_ReturnsNull_ForUnreadableDll()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "unreadable-mod");
        var dllPath = Path.Combine(modPath, "Unreadable.dll");
        
        Directory.CreateDirectory(Path.Combine(_sptPath, modPath));
        File.WriteAllBytes(Path.Combine(_sptPath, dllPath), [0x00, 0x01, 0x02]);

        var mod = _extractor.ExtractServerModMetadata(Path.Combine(_sptPath, dllPath), _sptPath);

        Assert.Null(mod);
    }

    [Fact]
    public void ExtractServerModMetadata_ReturnsNull_ForMalformedIL()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "malformed-il-mod");
        var dllPath = Path.Combine(modPath, "MalformedIL.dll");

        var code = @"
public abstract class AbstractModMetadata 
{
    public string ModGuid { get; set; }
}

public class MySptMod : AbstractModMetadata 
{
    public MySptMod() 
    {
        var x = ""com.server.test"";
    }
}
";
        _fixture.CompileDummyDll(dllPath, code);

        var mod = _extractor.ExtractServerModMetadata(Path.Combine(_sptPath, dllPath), _sptPath);

        Assert.Null(mod);
    }

    [Fact]
    public void ExtractServerModMetadata_ReturnsNull_ForNoSptMetadata()
    {
        var modPath = Path.Combine("SPT", "user", "mods", "no-metadata-mod");
        var dllPath = Path.Combine(modPath, "NoMetadata.dll");

        var code = @"
public class SomeClass 
{
    public string SomeProperty { get; set; }
}
";
        _fixture.CompileDummyDll(dllPath, code);

        var mod = _extractor.ExtractServerModMetadata(Path.Combine(_sptPath, dllPath), _sptPath);

        Assert.Null(mod);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}






