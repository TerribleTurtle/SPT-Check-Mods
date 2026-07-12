using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

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
    /// <returns>The workflow context, which contains the processed mods.</returns>
    Task<CheckModsExtended.Models.Pipeline.UpdateWorkflowContext> RunPipelineAsync(string[] args, CancellationToken cancellationToken = default);
}
