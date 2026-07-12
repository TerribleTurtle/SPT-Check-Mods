using CheckModsExtended.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckModsExtended.Tests.Utils;

/// <summary>
/// Tests for <see cref="BrowserLauncher"/>'s URL guard. Only the reject path is exercised.
/// </summary>
public sealed class BrowserLauncherTests
{
    [Fact]
    public void Tryopenurl_refuses_non_http_urls_ftp()
    {
        var launcher = new BrowserLauncher(
            NullLogger<BrowserLauncher>.Instance,
            new CheckModsExtended.Utils.ProcessRunner()
        );
        Assert.False(launcher.TryOpenUrl("ftp://example.com/file"));
    }

    [Fact]
    public void Tryopenurl_refuses_non_http_urls_file()
    {
        var launcher = new BrowserLauncher(
            NullLogger<BrowserLauncher>.Instance,
            new CheckModsExtended.Utils.ProcessRunner()
        );
        Assert.False(launcher.TryOpenUrl("file:///C:/secret"));
    }

    [Fact]
    public void Tryopenurl_refuses_non_http_urls_javascript()
    {
        var launcher = new BrowserLauncher(
            NullLogger<BrowserLauncher>.Instance,
            new CheckModsExtended.Utils.ProcessRunner()
        );
        Assert.False(launcher.TryOpenUrl("javascript:alert(1)"));
    }

    [Fact]
    public void Tryopenurl_refuses_non_http_urls_not_a_url()
    {
        var launcher = new BrowserLauncher(
            NullLogger<BrowserLauncher>.Instance,
            new CheckModsExtended.Utils.ProcessRunner()
        );
        Assert.False(launcher.TryOpenUrl("not a url"));
    }

    [Fact]
    public void Tryopenurl_refuses_non_http_urls_empty()
    {
        var launcher = new BrowserLauncher(
            NullLogger<BrowserLauncher>.Instance,
            new CheckModsExtended.Utils.ProcessRunner()
        );
        Assert.False(launcher.TryOpenUrl(""));
    }
}
