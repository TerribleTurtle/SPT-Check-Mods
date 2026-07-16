using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public class ModPartitionerTests
{
    private readonly ModPartitioner _sut = new();

    private PluginDll CreatePluginDll(string assemblyName, string guid, params string[] references)
    {
        return new PluginDll(
            "C:\\foo\\" + assemblyName + ".dll",
            new BepInPluginAttribute(guid, "Name", "1.0"),
            assemblyName,
            new HashSet<string>(references)
        );
    }

    [Fact]
    public void PartitionByRelatedness_SeparatesUnrelatedPlugins()
    {
        var plugins = new List<PluginDll>
        {
            CreatePluginDll("ModA", "com.author1.mod1"),
            CreatePluginDll("ModB", "com.author2.mod2")
        };

        var partitions = _sut.PartitionByRelatedness(plugins);

        Assert.Equal(2, partitions.Count);
        Assert.Single(partitions[0]);
        Assert.Single(partitions[1]);
    }

    [Fact]
    public void PartitionByRelatedness_GroupsByReferences()
    {
        var plugins = new List<PluginDll>
        {
            CreatePluginDll("ModA", "com.author1.moda"),
            CreatePluginDll("ModB", "com.author2.modb", "ModA")
        };

        var partitions = _sut.PartitionByRelatedness(plugins);

        Assert.Single(partitions);
        Assert.Equal(2, partitions[0].Count);
    }

    [Fact]
    public void PartitionByRelatedness_GroupsByAuthorNamespace()
    {
        var plugins = new List<PluginDll>
        {
            CreatePluginDll("ModA", "com.specificauthor.mod1"),
            CreatePluginDll("ModB", "com.specificauthor.mod2")
        };

        var partitions = _sut.PartitionByRelatedness(plugins);

        Assert.Single(partitions);
        Assert.Equal(2, partitions[0].Count);
    }

    [Fact]
    public void PartitionByRelatedness_DoesNotGroupByGenericNamespaceOnly()
    {
        var plugins = new List<PluginDll>
        {
            CreatePluginDll("ModA", "com.author1"),
            CreatePluginDll("ModB", "com.author2")
        };

        var partitions = _sut.PartitionByRelatedness(plugins);

        Assert.Equal(2, partitions.Count);
    }

    [Fact]
    public void PartitionByRelatedness_IgnoresNullGuids()
    {
        var plugins = new List<PluginDll>
        {
            CreatePluginDll("ModA", null!),
            CreatePluginDll("ModB", null!)
        };

        var partitions = _sut.PartitionByRelatedness(plugins);

        Assert.Equal(2, partitions.Count);
    }
}
