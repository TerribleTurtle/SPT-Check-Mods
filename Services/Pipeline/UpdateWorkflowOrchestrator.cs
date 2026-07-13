using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services.Pipeline;

/// <summary>
/// Orchestrates the execution of the update workflow pipeline.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class UpdateWorkflowOrchestrator : IUpdateWorkflowOrchestrator
{
    private readonly IEnumerable<IWorkflowStep> _steps;
    private readonly ILogger<UpdateWorkflowOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateWorkflowOrchestrator"/> class.
    /// </summary>
    /// <param name="steps">The workflow steps.</param>
    /// <param name="logger">The logger.</param>
    public UpdateWorkflowOrchestrator(IEnumerable<IWorkflowStep> steps, ILogger<UpdateWorkflowOrchestrator> logger)
    {
        _steps = steps;
        _logger = logger;
    }

    private static readonly SemaphoreSlim _pipelineLock = new(1, 1);

    /// <inheritdoc />
    public async Task<UpdateWorkflowContext> RunPipelineAsync(string[] args, CancellationToken cancellationToken = default)
    {
        await _pipelineLock.WaitAsync(cancellationToken);
        try
        {
            var context = new UpdateWorkflowContext { Args = args };

            foreach (var step in _steps)
            {
                await step.ExecuteAsync(context, cancellationToken);
                if (context.IsCancelled)
                {
                    break;
                }
            }

            return context;
        }
        finally
        {
            _pipelineLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<UpdateWorkflowContext> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        return await RunPipelineAsync(args, cancellationToken);
    }
}
