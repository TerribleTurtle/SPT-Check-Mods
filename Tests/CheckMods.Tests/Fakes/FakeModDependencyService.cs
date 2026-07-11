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
    public Task<DependencyAnalysisResult> AnalyzeDependenciesAsync(IEnumerable<Mod> mods, HashSet<string> installedModGuids, Action<int, int>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        if (progressCallback != null)
        {
            progressCallback(1, 1);
        }

        return Task.FromResult(ResultToReturn);
    }
}






