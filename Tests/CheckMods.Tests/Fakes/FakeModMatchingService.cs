using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

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
    public Task<Mod> MatchModAsync(Mod mod, Version sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (MatchModAction != null)
        {
            return Task.FromResult(MatchModAction(mod));
        }
        return Task.FromResult(mod);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Mod>> MatchModsAsync(IEnumerable<Mod> mods, Version sptVersion, Action<Mod, int, int>? progressCallback = null, CancellationToken cancellationToken = default)
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

            if (progressCallback != null)
            {
                progressCallback(matchedMod, i + 1, modList.Count);
            }
        }

        return Task.FromResult<IReadOnlyList<Mod>>(result);
    }
}






