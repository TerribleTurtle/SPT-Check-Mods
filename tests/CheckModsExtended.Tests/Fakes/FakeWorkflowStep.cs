using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeWorkflowStep : IWorkflowStep
{
    /// <summary>
    /// Optional action to execute during the step, allowing test mutation of context.
    /// </summary>
    public Action<UpdateWorkflowContext>? ExecuteAction { get; set; }

    /// <summary>
    /// Indicates whether the step was executed.
    /// </summary>
    public bool WasExecuted { get; private set; }

    /// <inheritdoc />
    public Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        WasExecuted = true;
        ExecuteAction?.Invoke(context);
        return Task.CompletedTask;
    }
}
