using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="ISptInstallationService"/>.
/// </summary>
public sealed class FakeSptInstallationService : ISptInstallationService
{
    private List<SptVersionResult> _updates = [];
    public bool ThrowsException { get; set; } = false;

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
        if (ThrowsException) throw new System.Exception("Simulated exception from FakeSptInstallationService");
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
