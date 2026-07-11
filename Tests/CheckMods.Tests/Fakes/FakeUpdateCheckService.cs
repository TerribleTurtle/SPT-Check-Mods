using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IUpdateCheckService"/>.
/// </summary>
public sealed class FakeUpdateCheckService : IUpdateCheckService
{
    /// <summary> Gets or sets result to return. </summary>
    public CheckModsUpdateResult ResultToReturn { get; set; } = new(CheckModsUpdateStatus.Unavailable, "1.0.0");

    /// <inheritdoc />
    public Task<CheckModsUpdateResult> CheckAsync(Version sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ResultToReturn);
    }
}
