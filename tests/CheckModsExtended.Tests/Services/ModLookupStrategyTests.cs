using System.Collections.Generic;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public class ModLookupStrategyTests
{
    [Fact]
    public async Task LookupModAsync_WhenGuidMatches_ReturnsMatchAnd100Confidence()
    {
        // Arrange
        var fakeClient = new FakeModSearchClient();
        var expectedResult = new ModSearchResult(1, null, "Test Mod", "slug", null, null, 0, null, null, null, null);
        fakeClient.OnGetModByGuid = guid =>
        {
            if (guid == "test.guid") return expectedResult;
            return new NoCompatibleVersion(expectedResult);
        };
        var strategy = new ModLookupStrategy(fakeClient);
        var mod = new Mod { Local = new LocalModIdentity { Guid = "test.guid", LocalName = "Test", FilePath = "", LocalAuthor = "", IsServerMod = false, LocalVersion = "1.0" } };
        var sptVersion = new SemanticVersioning.Version("3.9.0");

        // Act
        var result = await strategy.LookupModAsync(mod, sptVersion);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResult, result!.Value.Match);
        Assert.Equal(100, result.Value.ConfidenceScore);
    }
    
    [Fact]
    public async Task LookupModAsync_WhenSearchHasExactNameMatch_ReturnsMatchAnd100Confidence()
    {
        // Arrange
        var fakeClient = new FakeModSearchClient();
        var expectedResult = new ModSearchResult(1, null, "Test Mod", "slug", null, null, 0, null, null, null, null);
        fakeClient.OnSearch = term =>
        {
            if (term == "Test Mod") return new List<ModSearchResult> { expectedResult };
            return new List<ModSearchResult>();
        };
        var strategy = new ModLookupStrategy(fakeClient);
        var mod = new Mod { Local = new LocalModIdentity { Guid = "unmatched.guid", LocalName = "Test Mod", FilePath = "", LocalAuthor = "", IsServerMod = false, LocalVersion = "1.0" } };
        var sptVersion = new SemanticVersioning.Version("3.9.0");

        // Act
        var result = await strategy.LookupModAsync(mod, sptVersion);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResult, result!.Value.Match);
        Assert.Equal(100, result.Value.ConfidenceScore);
    }
}
