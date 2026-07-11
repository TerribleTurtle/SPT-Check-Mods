using CheckMods.Services.UI;
using Xunit;

namespace CheckMods.Tests.Services.UI;

/// <summary>
/// Tests for <see cref="UiFormattingUtility"/>
/// </summary>
public sealed class UiFormattingUtilityTests
{
    [Fact]
    public void islinkurlsafe_accepts_normal_urls_forge()
    {
        Assert.True(UiFormattingUtility.IsLinkUrlSafe("https://forge.sp-tarkov.com/mod/123/cool-mod"));
    }

    [Fact]
    public void islinkurlsafe_accepts_normal_urls_with_query()
    {
        Assert.True(UiFormattingUtility.IsLinkUrlSafe("https://example.com/path?query=1&x=2"));
    }

    [Fact]
    public void islinkurlsafe_rejects_null()
    {
        Assert.False(UiFormattingUtility.IsLinkUrlSafe(null));
    }

    [Fact]
    public void islinkurlsafe_rejects_empty()
    {
        Assert.False(UiFormattingUtility.IsLinkUrlSafe(""));
    }

    [Fact]
    public void islinkurlsafe_rejects_whitespace()
    {
        Assert.False(UiFormattingUtility.IsLinkUrlSafe("   "));
    }

    [Fact]
    public void islinkurlsafe_rejects_open_bracket()
    {
        Assert.False(UiFormattingUtility.IsLinkUrlSafe("https://example.com/a[b"));
    }

    [Fact]
    public void islinkurlsafe_rejects_close_bracket()
    {
        Assert.False(UiFormattingUtility.IsLinkUrlSafe("https://example.com/a]b"));
    }

    [Fact]
    public void islinkurlsafe_rejects_markup()
    {
        Assert.False(UiFormattingUtility.IsLinkUrlSafe("[red]not a url[/]"));
    }
}
