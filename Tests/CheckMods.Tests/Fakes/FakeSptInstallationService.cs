using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="ISptInstallationService"/>.
/// </summary>
public sealed class FakeSptInstallationService : ISptInstallationService
{
    private List<SptVersionResult> _updates = [];

    /// <summary> Gets or sets validated version. </summary>
    public Version? ValidatedVersion { get; set; }

    /// <summary> Gets or sets updates. </summary>
    public List<SptVersionResult> Updates
    {
        get { return _updates.ToList(); }
        set { _updates = value.ToList(); }
    }

    /// <inheritdoc />
    public Task<Version?> GetAndValidateSptVersionAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ValidatedVersion);
    }

    /// <inheritdoc />
    public Task<List<SptVersionResult>> CheckForSptUpdatesAsync(
        Version currentVersion,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_updates.ToList());
    }
}
