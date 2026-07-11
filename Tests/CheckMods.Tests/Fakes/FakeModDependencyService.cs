using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModDependencyService"/>.
/// </summary>
public sealed class FakeModDependencyService : IModDependencyService
{
    /// <summary>
    /// Gets or sets the result to return from <see cref="AnalyzeDependenciesAsync"/>.
    /// </summary>
    public DependencyAnalysisResult ResultToReturn { get; set; } = new DependencyAnalysisResult();

    /// <inheritdoc />
    public Task<(IReadOnlyList<Mod> UpdatedMods, DependencyAnalysisResult Result)> AnalyzeDependenciesAsync(
        IEnumerable<Mod> mods,
        ISet<string> installedModGuids,
        Action<int, int>? progressCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (progressCallback != null)
        {
            progressCallback(1, 1);
        }

        return Task.FromResult<(IReadOnlyList<Mod>, DependencyAnalysisResult)>((mods.ToList(), ResultToReturn));
    }
}
