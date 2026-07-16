using CheckModsExtended.Utils;

namespace CheckModsExtended.Tests.Utils;

/// <summary>
/// Tests for <see cref="ForgeUrls"/>, which builds links to the Forge website.
/// </summary>
public sealed class ForgeUrlsTests
{
    [Fact]
    public void Modpage_builds_detail_url()
    {
        Assert.Equal("https://forge.sp-tarkov.com/mod/123/cool-mod", ForgeUrls.ModPage(123, "cool-mod"));
    }

    [Fact]
    public void Download_builds_versioned_download_url()
    {
        Assert.Equal(
            "https://forge.sp-tarkov.com/mod/download/123/cool-mod/1.2.0",
            ForgeUrls.Download(123, "cool-mod", "1.2.0")
        );
    }

    [Fact]
    public void Modpage_with_missing_slug_yields_a_trailing_slash_url_null()
    {
        Assert.Equal("https://forge.sp-tarkov.com/mod/123/", ForgeUrls.ModPage(123, null));
    }

    [Fact]
    public void Modpage_with_missing_slug_yields_a_trailing_slash_url_empty()
    {
        Assert.Equal("https://forge.sp-tarkov.com/mod/123/", ForgeUrls.ModPage(123, ""));
    }
}
