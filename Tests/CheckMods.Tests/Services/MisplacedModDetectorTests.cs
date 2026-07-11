using System;
using System.IO;
using System.Linq;
using CheckMods.Configuration;
using CheckMods.Services;
using CheckMods.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CheckMods.Tests.Services;

public sealed class MisplacedModDetectorTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly MisplacedModDetector _detector;
    private readonly string _sptPath;

    public MisplacedModDetectorTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;
        var options = Options.Create(new ModScannerOptions { MaxDllSizeBytes = 10 * 1024 * 1024 });
        var pluginExtractor = new PluginMetadataExtractor(options, NullLogger<PluginMetadataExtractor>.Instance);
        var serverExtractor = new ServerModExtractor(NullLogger<ServerModExtractor>.Instance);
        _detector = new MisplacedModDetector(pluginExtractor, serverExtractor, NullLogger<MisplacedModDetector>.Instance);
    }

    [Fact]
    public void DetectMisplacedMods_IdentifiesMisplacedServerAndClientMods()
    {
        var misplacedClientPath = Path.Combine("SPT", "user", "mods", "wrong-client", "WrongClient.dll");
        var clientCode = @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.wrong.client"", ""Wrong Client"", ""1.0"")]
public class Plugin {}
";
        _fixture.CompileDummyDll(misplacedClientPath, clientCode);

        var misplacedServerPath = Path.Combine("BepInEx", "plugins", "wrong-server", "WrongServer.dll");
        var serverCode = @"
public abstract class AbstractModMetadata 
{
    public string ModGuid { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string Version { get; set; }
    public string SptVersion { get; set; }
}
public class WrongServerMod : AbstractModMetadata 
{
    public WrongServerMod() { ModGuid = ""com.wrong.server""; Name = ""Wrong""; Author = ""Author""; Version = ""1.0""; }
}
";
        _fixture.CompileDummyDll(misplacedServerPath, serverCode);

        var report = _detector.DetectMisplacedMods(_sptPath);

        Assert.Equal(2, report.WrongFolder.Count);
        
        var wrongClient = report.WrongFolder.Single(m => m.Guid == "com.wrong.client");
        Assert.False(wrongClient.IsServerMod);

        var wrongServer = report.WrongFolder.Single(m => m.Guid == "com.wrong.server");
        Assert.True(wrongServer.IsServerMod);
    }

    [Fact]
    public void DetectMisplacedMods_DetectsCrossInstalledDirectories()
    {
        var dirPath = Path.Combine("BepInEx", "plugins", "cross-installed");
        
        var clientCode1 = @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.author1.mod"", ""Author1 Mod"", ""1.0"")]
public class Plugin1 {}
";
        _fixture.CompileDummyDll(Path.Combine(dirPath, "Mod1.dll"), clientCode1);

        var clientCode2 = @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.author2.mod"", ""Author2 Mod"", ""1.0"")]
public class Plugin2 {}
";
        _fixture.CompileDummyDll(Path.Combine(dirPath, "Mod2.dll"), clientCode2);

        var report = _detector.DetectMisplacedMods(_sptPath);

        Assert.Single(report.CrossInstalled);
        var crossInstall = report.CrossInstalled[0];
        Assert.True(crossInstall.Ambiguous);
        Assert.Equal(2, crossInstall.Mods.Count);
    }

    [Fact]
    public void DetectMisplacedMods_DetectsCrossInstalledDirectories_Unambiguous()
    {
        var dirPath = Path.Combine("BepInEx", "plugins", "unambiguous-mod");
        
        var clientCode1 = @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.author1.unambiguous"", ""Unambiguous Mod"", ""1.0"")]
public class Plugin1 {}
";
        _fixture.CompileDummyDll(Path.Combine(dirPath, "UnambiguousMod.dll"), clientCode1);

        var clientCode2 = @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.author2.other"", ""Other Mod"", ""1.0"")]
public class Plugin2 {}
";
        _fixture.CompileDummyDll(Path.Combine(dirPath, "Other.dll"), clientCode2);

        var report = _detector.DetectMisplacedMods(_sptPath);

        Assert.Single(report.CrossInstalled);
        var crossInstall = report.CrossInstalled[0];
        Assert.False(crossInstall.Ambiguous);
        Assert.Equal(2, crossInstall.Mods.Count);
        Assert.Single(crossInstall.Misplaced);
        Assert.Equal("com.author2.other", crossInstall.Misplaced[0].Guid);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}






