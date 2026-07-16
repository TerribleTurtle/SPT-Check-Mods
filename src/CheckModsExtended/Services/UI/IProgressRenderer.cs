using System;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.UI;

/// <summary>
/// Renders progress bars and spinners.
/// </summary>
public interface IProgressRenderer
{
    /// <summary>Runs work under a Forge-query progress bar, passing a callback to report completed-item counts.</summary>
    Task RunForgeQueryProgressAsync(
        int total,
        Func<Action<int>, Task> work,
        CancellationToken cancellationToken = default
    );

    /// <summary>Runs work under a Forge-query progress bar and returns its result.</summary>
    Task<T> RunForgeQueryProgressAsync<T>(
        int total,
        Func<Action<int>, Task<T>> work,
        CancellationToken cancellationToken = default
    );
}
