namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Opens URLs in the user's default browser.
/// </summary>
public interface IBrowserLauncher
{
    /// <summary>
    /// Attempts to open <paramref name="url"/> in the default browser, returning false on any failure or a non-http(s) URL.
    /// </summary>
    OneOf.OneOf<OneOf.Types.Success, CheckModsExtended.Models.ApiError> TryOpenUrl(string url);
}
