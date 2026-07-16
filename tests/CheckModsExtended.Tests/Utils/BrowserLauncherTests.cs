using CheckModsExtended.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CheckModsExtended.Tests.Utils;

/// <summary>
/// Tests for <see cref="BrowserLauncher"/>'s URL guard. Only the reject path is exercised.
/// </summary>
public sealed class BrowserLauncherTests
{
    [Fact]
    public void Tryopenurl_invalid_throws_and_returns_error()
    {
        var launcher = new BrowserLauncher(
            NullLogger<BrowserLauncher>.Instance,
            new FakeProcessRunner()
        );
        var res = launcher.TryOpenUrl("|||invalid|||");
        Assert.True(res.IsT1);
    }

    private sealed class FakeProcessRunner : CheckModsExtended.Utils.IProcessRunner
    {
        public System.Diagnostics.Process? Start(System.Diagnostics.ProcessStartInfo startInfo)
        {
            throw new System.ComponentModel.Win32Exception("Fake exception");
        }
    }
}
