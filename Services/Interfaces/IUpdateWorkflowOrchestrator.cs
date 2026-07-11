using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;

namespace CheckMods.Services.Interfaces;

/// <summary>
/// Orchestrates the execution of the update workflow pipeline.
/// </summary>
public interface IUpdateWorkflowOrchestrator
{
    /// <summary>
    /// Runs the update pipeline with the given arguments.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of processed mods.</returns>
    Task<IReadOnlyList<Mod>> RunPipelineAsync(string[] args, CancellationToken cancellationToken = default);
}
