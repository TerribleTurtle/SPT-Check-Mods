using System;
using System.IO;
using System.Linq;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fixtures;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class BinaryParserTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly BinaryParser _parser;
    private readonly string _sptPath;

    public BinaryParserTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;
        _parser = new BinaryParser(_fixture.FileSystem);
    }

    [Fact]
    public void Extractbepinplugin_returnsplugin_forvaliddll()
    {
        var dllPath = Path.Combine(_sptPath, "ValidClient.dll");
        var code = @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.valid.plugin"", ""Valid Plugin"", ""1.0.0"")]
public class ValidPlugin {}
";
        _fixture.CompileDummyDll(dllPath, code);

        var plugin = _parser.ExtractBepInPlugin(dllPath);

        Assert.NotNull(plugin);
        Assert.Equal("com.valid.plugin", plugin!.Guid);
        Assert.Equal("Valid Plugin", plugin.Name);
        Assert.Equal("1.0.0", plugin.Version?.ToString());
    }

    [Fact]
    public void Extractservermodmetadata_returnsmetadata_forvaliddll()
    {
        var dllPath = Path.Combine(_sptPath, "ValidServer.dll");
        var code = @"
using System;
public abstract class AbstractModMetadata { }
public class ValidModMetadata : AbstractModMetadata 
{
    public string ModGuid { get; } = ""Author-ServerMod"";
    public string Name { get; } = ""Server Mod"";
    public string Author { get; } = ""Author"";
    public string Version { get; } = ""2.0.0"";
    public string SptVersion { get; } = ""3.8.0"";
}
";
        _fixture.CompileDummyDll(dllPath, code);

        var metadata = _parser.ExtractServerModMetadata(dllPath);

        Assert.NotNull(metadata);
        Assert.Equal("Author-ServerMod", metadata!.Guid);
        Assert.Equal("Server Mod", metadata.Name);
        Assert.Equal("2.0.0", metadata.Version);
        Assert.Equal("3.8.0", metadata.SptVersion);
    }

    [Fact]
    public void Extractbepinplugin_throwsbadimageformatexception_forcorrupteddll()
    {
        var dllPath = Path.Combine(_sptPath, "Corrupted.dll");
        _fixture.FileSystem.Files[dllPath] = new byte[] { 0x00, 0x01, 0x02, 0x03 }; // Invalid PE

        Assert.Throws<BadImageFormatException>(() => _parser.ExtractBepInPlugin(dllPath));
    }

    [Fact]
    public void Extractservermodmetadata_throwsbadimageformatexception_forcorrupteddll()
    {
        var dllPath = Path.Combine(_sptPath, "CorruptedServer.dll");
        _fixture.FileSystem.Files[dllPath] = new byte[] { 0x00, 0x01, 0x02, 0x03 }; // Invalid PE

        Assert.Throws<BadImageFormatException>(() => _parser.ExtractServerModMetadata(dllPath));
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}
