using CheckModsExtended.Services.Web;
using Xunit;

namespace CheckModsExtended.Tests.Services.Web;

public class ModDtoTests
{
    [Fact]
    public void ModDto_IncludesLocalDirectory()
    {
        var dto = new ModDto(
            Id: 1,
            Name: "Test",
            Author: "Author",
            LocalVersion: "1.0",
            LatestVersion: "2.0",
            Status: "UpdateAvailable",
            IsServerMod: true,
            ModUrl: "http://example.com",
            DownloadUrl: "http://example.com/dl",
            LocalDirectory: "C:\\path\\to\\dir"
        );

        Assert.Equal("C:\\path\\to\\dir", dto.LocalDirectory);
    }
}

