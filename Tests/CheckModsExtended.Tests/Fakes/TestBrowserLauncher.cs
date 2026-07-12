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

    public Task<string> WaitForUrlAsync() => _tcs.Task;

    public bool TryOpenUrl(string url)
    {
        _tcs.TrySetResult(url);
        return true;
    }
}
