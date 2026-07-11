using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IUpdateCheckService"/>.
/// </summary>
public sealed class FakeUpdateCheckService : IUpdateCheckService
{
    /// <summary> Gets or sets result to return. </summary>
    public CheckModsExtendedUpdateResult ResultToReturn { get; set; } = new(CheckModsExtendedUpdateStatus.Unavailable, "1.0.0");
    public Exception? CheckAsyncThrows { get; set; }

    /// <inheritdoc />
    public Task<CheckModsExtendedUpdateResult> CheckAsync(Version sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (CheckAsyncThrows is null)
        {
            return Task.FromResult(ResultToReturn);
        }
        throw CheckAsyncThrows;
    }
}

