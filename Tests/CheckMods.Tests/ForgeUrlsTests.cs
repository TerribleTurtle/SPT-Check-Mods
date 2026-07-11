using CheckMods.Utils;

namespace CheckMods.Tests;

/// <summary>
/// Tests for <see cref="ForgeUrls"/>, which builds links to the Forge website.
/// </summary>
public sealed class ForgeUrlsTests
{
    [Fact]
    public void modpage_builds_detail_url()
    {
        Assert.Equal("https://forge.sp-tarkov.com/mod/123/cool-mod", ForgeUrls.ModPage(123, "cool-mod"));
    }

    [Fact]
    public void download_builds_versioned_download_url()
    {
        Assert.Equal(
            "https://forge.sp-tarkov.com/mod/download/123/cool-mod/1.2.0",
            ForgeUrls.Download(123, "cool-mod", "1.2.0")
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void modpage_with_missing_slug_yields_a_trailing_slash_url(string? slug)
    {
        Assert.Equal("https://forge.sp-tarkov.com/mod/123/", ForgeUrls.ModPage(123, slug));
    }
}
