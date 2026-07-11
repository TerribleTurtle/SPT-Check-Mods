using CheckMods.Models;
using CheckMods.Services.UI;
namespace CheckMods.Tests;

/// <summary>
/// Tests for <see cref="TableRenderer"/>'s URL safety guard. A URL embedded in a Spectre [link=...] tag
/// must not contain the markup delimiters '[' or ']', or rendering throws and aborts the run.
/// </summary>
public sealed class TableRendererTests
{
    [Fact]
    public void IsLinkUrlSafe_accepts_normal_urls_forge()
    {
        Assert.True(TableRenderer.IsLinkUrlSafe("https://forge.sp-tarkov.com/mod/123/cool-mod"));
    }

    [Fact]
    public void IsLinkUrlSafe_accepts_normal_urls_with_query()
    {
        Assert.True(TableRenderer.IsLinkUrlSafe("https://example.com/path?query=1&x=2"));
    }

    [Fact]
    public void IsLinkUrlSafe_rejects_null()
    {
        Assert.False(TableRenderer.IsLinkUrlSafe(null));
    }

    [Fact]
    public void IsLinkUrlSafe_rejects_empty()
    {
        Assert.False(TableRenderer.IsLinkUrlSafe(""));
    }

    [Fact]
    public void IsLinkUrlSafe_rejects_whitespace()
    {
        Assert.False(TableRenderer.IsLinkUrlSafe("   "));
    }

    [Fact]
    public void IsLinkUrlSafe_rejects_open_bracket()
    {
        Assert.False(TableRenderer.IsLinkUrlSafe("https://example.com/a[b"));
    }

    [Fact]
    public void IsLinkUrlSafe_rejects_close_bracket()
    {
        Assert.False(TableRenderer.IsLinkUrlSafe("https://example.com/a]b"));
    }

    [Fact]
    public void IsLinkUrlSafe_rejects_markup()
    {
        Assert.False(TableRenderer.IsLinkUrlSafe("[red]not a url[/]"));
    }

    [Fact]
    public void FormatVersionDisplay_formats_ignored_update()
    {
        var mod = CreateMod();
        typeof(Mod).GetProperty(nameof(Mod.LatestVersion))!.SetValue(mod, "2.0.0");
        mod.SetUpdateSuppressed(true);

        var result = TableRenderer.FormatVersionDisplay(mod);

        Assert.Equal("[grey]2.0.0 (ignored)[/]", result);
    }

    [Fact]
    public void FormatVersionDisplay_formats_up_to_date()
    {
        var mod = CreateMod();
        typeof(Mod).GetProperty(nameof(Mod.LatestVersion))!.SetValue(mod, "1.0.0");
        typeof(Mod).GetProperty(nameof(Mod.UpdateStatus))!.SetValue(mod, UpdateStatus.UpToDate);

        var result = TableRenderer.FormatVersionDisplay(mod);

        Assert.Equal("[green]1.0.0[/]", result);
    }

    [Fact]
    public void FormatVersionDisplay_formats_update_available()
    {
        var mod = CreateMod();
        typeof(Mod).GetProperty(nameof(Mod.LatestVersion))!.SetValue(mod, "2.0.0");
        typeof(Mod).GetProperty(nameof(Mod.UpdateStatus))!.SetValue(mod, UpdateStatus.UpdateAvailable);

        var result = TableRenderer.FormatVersionDisplay(mod);

        Assert.Equal("[red]2.0.0[/]", result);
    }

    [Fact]
    public void FormatVersionDisplay_formats_update_blocked()
    {
        var mod = CreateMod();
        typeof(Mod).GetProperty(nameof(Mod.LatestVersion))!.SetValue(mod, "2.0.0");
        typeof(Mod).GetProperty(nameof(Mod.UpdateStatus))!.SetValue(mod, UpdateStatus.UpdateBlocked);

        var result = TableRenderer.FormatVersionDisplay(mod);

        Assert.Equal("[darkorange]2.0.0[/]", result);
    }

    [Fact]
    public void FormatVersionDisplay_formats_newer_installed()
    {
        var mod = CreateMod();
        typeof(Mod).GetProperty(nameof(Mod.LatestVersion))!.SetValue(mod, "0.9.0");
        typeof(Mod).GetProperty(nameof(Mod.UpdateStatus))!.SetValue(mod, UpdateStatus.NewerInstalled);

        var result = TableRenderer.FormatVersionDisplay(mod);

        Assert.Equal("[blue]0.9.0[/]", result);
    }

    [Fact]
    public void FormatVersionDisplay_formats_unknown_status()
    {
        var mod = CreateMod();
        typeof(Mod).GetProperty(nameof(Mod.LatestVersion))!.SetValue(mod, "1.0.0");
        typeof(Mod).GetProperty(nameof(Mod.UpdateStatus))!.SetValue(mod, UpdateStatus.Unknown);

        var result = TableRenderer.FormatVersionDisplay(mod);

        Assert.Equal("1.0.0", result);
    }

    private static Mod CreateMod()
    {
        return new Mod
        {
            Guid = "test",
            FilePath = "test.dll",
            IsServerMod = false,
            LocalName = "test",
            LocalAuthor = "test",
            LocalVersion = "1.0.0"
        };
    }
}
