using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModMatchingService"/>.
/// </summary>
public sealed class FakeModMatchingService : IModMatchingService
{
    /// <summary>
    /// Gets or sets the action to perform on each matched mod.
    /// </summary>
    public Func<Mod, Mod>? MatchModAction { get; set; }

    /// <inheritdoc />
    public Task<(Mod Mod, PendingConfirmation? Confirmation)> MatchModAsync(
        Mod mod,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (MatchModAction != null)
        {
            return Task.FromResult((MatchModAction(mod), (PendingConfirmation?)null));
        }
        return Task.FromResult((mod, (PendingConfirmation?)null));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Mod>> MatchModsAsync(
        IEnumerable<Mod> mods,
        Version sptVersion,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var modList = mods.ToList();
        var result = new List<Mod>();

        for (int i = 0; i < modList.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var m = modList[i];

            var matchedMod = MatchModAction != null ? MatchModAction(m) : m;
            result.Add(matchedMod);

            if (progress != null)
            {
                progress.Report(i + 1);
            }
        }

        return Task.FromResult<IReadOnlyList<Mod>>(result);
    }
}
