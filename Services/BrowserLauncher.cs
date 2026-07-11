using System.Diagnostics;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Default <see cref="IBrowserLauncher"/>. Opens URLs via the OS shell.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class BrowserLauncher(ILogger<BrowserLauncher> logger) : IBrowserLauncher
{
    /// <inheritdoc />
    public bool TryOpenUrl(string url)
    {
        // Only passes http(s) URLs to the shell.
        if (
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        )
        {
            logger.LogWarning("Refusing to open non-http(s) URL");
            return false;
        }

        try
        {
            // UseShellExecute lets the OS pick the default browser; the returned process may be null.
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex)
            when (ex is System.ComponentModel.Win32Exception or ObjectDisposedException or FileNotFoundException)
        {
            logger.LogWarning(ex, "Could not open the browser");
            return false;
        }
    }
}

