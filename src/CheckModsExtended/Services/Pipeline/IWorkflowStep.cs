using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;

namespace CheckModsExtended.Services.Pipeline;

/// <summary>
/// Represents a single step in the update workflow pipeline.
/// </summary>
public interface IWorkflowStep
{
    /// <summary>
    /// Executes the workflow step.
    /// </summary>
    /// <param name="context">The workflow context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken);
}
