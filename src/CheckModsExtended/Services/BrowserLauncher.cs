using System.Diagnostics;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

/// <summary>
/// Default <see cref="IBrowserLauncher"/>. Opens URLs via the OS shell.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class BrowserLauncher(
    ILogger<BrowserLauncher> logger,
    CheckModsExtended.Utils.IProcessRunner processRunner
) : IBrowserLauncher
{
    /// <inheritdoc />
    public OneOf.OneOf<OneOf.Types.Success, CheckModsExtended.Models.ApiError> TryOpenUrl(string url)
    {
        try
        {
            processRunner.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return new OneOf.Types.Success();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            logger.LogWarning(ex, "Could not open the browser");
            return new CheckModsExtended.Models.ApiError($"Failed to open target: {ex.Message}");
        }
        catch (System.IO.FileNotFoundException ex)
        {
            logger.LogWarning(ex, "Could not open the browser");
            return new CheckModsExtended.Models.ApiError($"Target not found: {ex.Message}");
        }
        catch (System.ObjectDisposedException ex)
        {
            logger.LogWarning(ex, "Could not open the browser");
            return new CheckModsExtended.Models.ApiError($"Process disposed: {ex.Message}");
        }
    }
}
