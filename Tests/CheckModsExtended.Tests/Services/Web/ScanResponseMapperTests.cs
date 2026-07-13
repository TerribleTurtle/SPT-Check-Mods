using System.Collections.Generic;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Web;
using Xunit;
using CheckModsExtended.Models;

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
}
