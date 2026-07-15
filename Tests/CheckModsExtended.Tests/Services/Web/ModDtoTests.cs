using CheckModsExtended.Services.Web;
using Xunit;

namespace CheckModsExtended.Tests.Services.Web;

public class ModDtoTests
{
    [Fact]
    public void ModDto_IncludesLocalDirectory()
    {
        var localDir = "C:\\path\\to\\dir";
        var dto = new ModDto(
            Id: 1,
            Name: "Test Mod",
            LocalName: "Test Mod",
            Author: "Author",
            LocalVersion: "1.0.0",
            LatestVersion: "1.0.0",
            Status: "UpToDate",
            IsServerMod: false,
            ModUrl: "https://example.com/mod",
            DownloadUrl: "https://example.com/download",
            LocalDirectory: localDir
        );

        Assert.Equal("C:\\path\\to\\dir", dto.LocalDirectory);
    }
}
