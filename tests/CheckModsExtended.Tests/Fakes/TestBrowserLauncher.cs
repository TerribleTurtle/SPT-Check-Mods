using System;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// A mocked browser launcher that prevents the system's browser from popping open
/// during automated tests, while capturing the launched URL.
/// </summary>
public sealed class TestBrowserLauncher : IBrowserLauncher
{
    private readonly TaskCompletionSource<string> _tcs = new();

    public Task<string> WaitForUrlAsync()
    {
        return _tcs.Task;
    }

    public OneOf.OneOf<OneOf.Types.Success, CheckModsExtended.Models.ApiError> TryOpenUrl(string url)
    {
        _tcs.TrySetResult(url);
        if (url.Contains("invalid"))
        {
            return new CheckModsExtended.Models.ApiError("Invalid URL");
        }

        return new OneOf.Types.Success();
    }
}
