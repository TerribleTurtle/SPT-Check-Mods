using System;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Pipeline;

namespace CheckMods.Tests.Fakes;

public sealed class FakeWorkflowStep : IWorkflowStep
{
    public Action<UpdateWorkflowContext>? ExecuteAction { get; set; }
    public bool WasExecuted { get; private set; }

    public Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        WasExecuted = true;
        ExecuteAction?.Invoke(context);
        return Task.CompletedTask;
    }
}
