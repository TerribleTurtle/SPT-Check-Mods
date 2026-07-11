using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IRemoteIgnoreFileClient"/>.
/// </summary>
public sealed class FakeRemoteIgnoreFileClient : IRemoteIgnoreFileClient
{
    /// <summary> Gets or sets whether it is configured. </summary>
    public bool IsConfigured { get; set; } = true;

    /// <summary> Gets or sets result to return. </summary>
    public IReadOnlyList<IgnoredUpdate>? ResultToReturn { get; set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<IgnoredUpdate>?> FetchAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ResultToReturn);
    }
}
