using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class PluginMetadataExtractorTests : IDisposable
{
    private readonly SptSandboxFixture _fixture;
    private readonly PluginMetadataExtractor _extractor;
    private readonly string _sptPath;

    public PluginMetadataExtractorTests()
    {
        _fixture = new SptSandboxFixture();
        _sptPath = _fixture.SandboxPath;
        var options = Options.Create(new ModScannerOptions { MaxDllSizeBytes = 10 * 1024 * 1024 });
        _extractor = new PluginMetadataExtractor(new ModPartitioner(), options, NullLogger<PluginMetadataExtractor>.Instance);
    }

    [Fact]
    public async Task Processclientdllsinparallelasync_returnsvalidmods()
    {
        var dllPath = Path.Combine("BepInEx", "plugins", "test-client-mod", "TestClient.dll");

        var code =
            @"
using System;
public class BepInPluginAttribute : Attribute 
{
    public BepInPluginAttribute(string guid, string name, string version) {}
}

[BepInPlugin(""com.client.test"", ""Test Client Mod"", ""1.0.0"")]
public class MyClientPlugin {}
";
        _fixture.CompileDummyDll(dllPath, code);

        var mods = await _extractor.ProcessClientDllsInParallelAsync([Path.Combine(_sptPath, dllPath)]);

        Assert.Single(mods);
        var mod = mods[0];
        Assert.False(mod.Local.IsServerMod);
        Assert.Equal("com.client.test", mod.Local.Guid);
        Assert.Equal("Test Client Mod", mod.Local.LocalName);
        Assert.Equal("client", mod.Local.LocalAuthor);
        Assert.Equal("1.0.0", mod.Local.LocalVersion);
    }

    [Fact]
    public async Task Trydetectclientmodasync_returnsmod_whenvaliddll()
    {
        var dllPath = Path.Combine("BepInEx", "plugins", "trydetect-client-mod", "TestClient.dll");
        var code =
            @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.trydetect.test"", ""Try Detect Client Mod"", ""1.0.0"")]
public class TryDetectClientPlugin {}
";
        _fixture.CompileDummyDll(dllPath, code);

        var mod = await _extractor.TryDetectClientModAsync(Path.Combine(_sptPath, dllPath));

        Assert.NotNull(mod);
        Assert.Equal("com.trydetect.test", mod!.Local.Guid);
    }

    [Fact]
    public async Task Readplugindllsasync_returnsplugindlls()
    {
        var dllPath = Path.Combine("BepInEx", "plugins", "readplugins-client-mod", "TestClient.dll");
        var code =
            @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.readplugins.test"", ""Read Plugins Mod"", ""1.0.0"")]
public class ReadPluginsPlugin {}
";
        _fixture.CompileDummyDll(dllPath, code);

        var plugins = await _extractor.ReadPluginDllsAsync([Path.Combine(_sptPath, dllPath)]);

        Assert.Single(plugins);
        Assert.Equal("com.readplugins.test", plugins[0].Plugin.Guid);
    }

    [Fact]
    public async Task Partitionbyrelatedness_partitionscorrectly()
    {
        var dllPath1 = Path.Combine("BepInEx", "plugins", "partition-client-mod", "Core.dll");
        var code1 =
            @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.partition.core"", ""Partition Core Mod"", ""1.0.0"")]
public class CorePlugin {}
";
        _fixture.CompileDummyDll(dllPath1, code1);

        var dllPath2 = Path.Combine("BepInEx", "plugins", "partition-client-mod", "Module.dll");
        var code2 =
            @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.partition.module"", ""Partition Module Mod"", ""1.0.0"")]
public class ModulePlugin {}
";
        _fixture.CompileDummyDll(dllPath2, code2);

        var plugins = await _extractor.ReadPluginDllsAsync([Path.Combine(_sptPath, dllPath1), Path.Combine(_sptPath, dllPath2)]);
        var partitioned = new ModPartitioner().PartitionByRelatedness(plugins);

        Assert.Single(partitioned); // Same author namespace 'com.partition'
        Assert.Equal(2, partitioned[0].Count);
    }

    [Fact]
    public async Task Consolidatedirectorymodsasync_returnsconsolidatedmods()
    {
        var dirName = "consolidate-client-mod";
        var dirPath = Path.Combine(_sptPath, "BepInEx", "plugins", dirName);
        var dllPath1 = Path.Combine("BepInEx", "plugins", dirName, "Core.dll");
        var code1 =
            @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.consolidate.core"", ""Consolidate Core Mod"", ""1.0.0"")]
public class CorePlugin {}
";
        _fixture.CompileDummyDll(dllPath1, code1);

        var dllPath2 = Path.Combine("BepInEx", "plugins", dirName, "Module.dll");
        var code2 =
            @"
using System;
public class BepInPluginAttribute : Attribute { public BepInPluginAttribute(string g, string n, string v) {} }
[BepInPlugin(""com.consolidate.module"", ""Consolidate Module Mod"", ""1.0.0"")]
public class ModulePlugin {}
";
        _fixture.CompileDummyDll(dllPath2, code2);

        var consolidated = await _extractor.ConsolidateDirectoryModsAsync(
            dirPath,
            [Path.Combine(_sptPath, dllPath1), Path.Combine(_sptPath, dllPath2)]
        );

        Assert.Single(consolidated);
        var mod = consolidated[0];
        Assert.Equal("com.consolidate.core", mod.Local.Guid);
        Assert.Single(mod.Local.AlternateGuids);
        Assert.Contains("com.consolidate.module", mod.Local.AlternateGuids);
    }

    [Fact]
    public void Getvalidclientdllfiles_filterscorrectly()
    {
        var pluginsPath = Path.Combine(_sptPath, "BepInEx", "plugins");
        Directory.CreateDirectory(pluginsPath);

        var validDll = Path.Combine(pluginsPath, "Valid.dll");
        File.WriteAllBytes(validDll, new byte[100]);

        var sptDir = Path.Combine(pluginsPath, "spt");
        Directory.CreateDirectory(sptDir);
        var sptDll = Path.Combine(sptDir, "SptCore.dll");
        File.WriteAllBytes(sptDll, new byte[100]);

        var largeDll = Path.Combine(pluginsPath, "Large.dll");
        File.WriteAllBytes(largeDll, new byte[10 * 1024 * 1024 + 1]);

        var validDlls = _extractor.GetValidClientDllFiles(pluginsPath);

        Assert.Single(validDlls);
        Assert.Equal(validDll, validDlls[0]);
    }

    [Fact]
    public async Task Trydetectclientmodasync_returnsnull_wheninvaliddll()
    {
        var dllPath = Path.Combine(_sptPath, "BepInEx", "plugins", "Invalid.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(dllPath)!);
        File.WriteAllBytes(dllPath, [0x00, 0x01, 0x02]);

        var mod = await _extractor.TryDetectClientModAsync(dllPath);

        Assert.Null(mod);
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }
}

