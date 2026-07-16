using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Web;
using Xunit;

namespace CheckModsExtended.Tests.Services.Web;

public class ScanResponseMapperTests
{
    [Fact]
    public void Map_WithEmptyContext_ReturnsEmptyScanResponse()
    {
        // Arrange
        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod>(),
            SptVersion = null,
            SptPath = "C:\\SPT",
            MisplacedReport = null
        };

        // Act
        var result = ScanResponseMapper.Map(context);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Mods);
        Assert.Null(result.MisplacedMods);
        Assert.Null(result.SptVersion);
    }

    [Fact]
    public void Map_WithPopulatedContext_ReturnsMappedResponse()
    {
        // Arrange
        var context = new UpdateWorkflowContext
        {
            Args = [],
            SptVersion = new SemanticVersioning.Version("3.9.0"),
            MisplacedReport = new MisplacedModReport(
                new List<MisplacedMod>
                {
                    new MisplacedMod(false, "guid1", "BadMod", "1.0", "wrong/path")
                },
                new List<CrossInstalledDirectory>
                {
                    new CrossInstalledDirectory(
                        "cross/dir",
                        new List<MisplacedMod>
                        {
                            new MisplacedMod(true, "guid2", "CrossMod", "1.0", "cross/dir/mod")
                        },
                        new List<MisplacedMod>
                        {
                            new MisplacedMod(true, "guid2", "CrossMod", "1.0", "cross/dir/mod")
                        },
                        true
                    )
                }
            ),
            Mods = new List<Mod>
            {
                new Mod
                {
                    Local = new LocalModIdentity
                    {
                        Guid = "mod-guid",
                        LocalName = "TestMod",
                        LocalAuthor = "Author",
                        LocalVersion = "1.0.0",
                        IsServerMod = true,
                        FilePath = "path/to/mod"
                    },
                    Api = new ForgeApiMetadata
                    {
                        ApiModId = 123,
                        ApiUrl = "http://example.com"
                    },
                    Update = new ModUpdateState
                    {
                        LatestVersion = "2.0.0",
                        UpdateStatus = UpdateStatus.UpdateAvailable,
                        DownloadLink = "http://example.com/dl",
                        UpdateDependencyChanges = new UpdateDependencyDelta(
                            new List<DependencyChange>
                            {
                                new DependencyChange
                                {
                                    ModId = 999,
                                    Guid = "dep-guid",
                                    Slug = "dep-slug",
                                    Name = "Dep Mod",
                                    RecommendedVersion = "1.0",
                                    InstallState = DependencyInstallState.NotInstalled,
                                    Conflict = false,
                                    DownloadLink = "http://dl"
                                }
                            },
                            new List<DependencyChange>()
                        )
                    },
                    Status = ModStatus.Verified
                }
            }
        };

        // Act
        var result = ScanResponseMapper.Map(context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("3.9.0", result.SptVersion);
        
        Assert.NotNull(result.MisplacedMods);
        Assert.Single(result.MisplacedMods.WrongFolder);
        Assert.Equal("BadMod", result.MisplacedMods.WrongFolder[0].Name);
        
        Assert.Single(result.MisplacedMods.CrossInstalled);
        Assert.Equal("cross/dir", result.MisplacedMods.CrossInstalled[0].Directory);
        Assert.Single(result.MisplacedMods.CrossInstalled[0].Mods);
        Assert.True(result.MisplacedMods.CrossInstalled[0].Ambiguous);
        
        Assert.Single(result.Mods);
        var mappedMod = result.Mods[0];
        Assert.Equal(123, mappedMod.Id);
        Assert.Equal("TestMod", mappedMod.Name);
        Assert.Equal("1.0.0", mappedMod.LocalVersion);
        Assert.Equal("2.0.0", mappedMod.LatestVersion);
        Assert.Equal("UpdateAvailable", mappedMod.Status);
        Assert.True(mappedMod.IsServerMod);
        Assert.Equal("http://example.com", mappedMod.ModUrl);
        Assert.Equal("http://example.com/dl", mappedMod.DownloadUrl);
        Assert.NotNull(mappedMod.AddedDependencies);
        Assert.Single(mappedMod.AddedDependencies);
        Assert.Equal(999, mappedMod.AddedDependencies[0].ModId);
        Assert.Equal("dep-slug", mappedMod.AddedDependencies[0].Slug);
    }
}
