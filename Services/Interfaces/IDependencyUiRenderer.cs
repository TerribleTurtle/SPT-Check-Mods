using System.Collections.Generic;
using CheckMods.Models;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// UI Renderer for displaying mod dependency analysis results.
/// </summary>
public interface IDependencyUiRenderer
{
    /// <summary>
    /// Renders the full dependency analysis results including tree, conflicts, and missing dependencies.
    /// </summary>
    /// <param name="result">The analysis results.</param>
    void DependencyResults(DependencyAnalysisResult result);
}
