using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

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
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (progress != null)
        {
            progress.Report(1);
        }

        return Task.FromResult<(IReadOnlyList<Mod>, DependencyAnalysisResult)>((mods.ToList(), ResultToReturn));
    }
}
