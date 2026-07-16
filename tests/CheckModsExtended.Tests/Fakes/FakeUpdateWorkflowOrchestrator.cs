using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeUpdateWorkflowOrchestrator : IUpdateWorkflowOrchestrator
{
    public bool ShouldThrow { get; set; } = false;
    public UpdateWorkflowContext ContextToReturn { get; set; } = new UpdateWorkflowContext { Args = new string[0] };

    public Task<UpdateWorkflowContext> RunPipelineAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (ShouldThrow)
        {
            throw new System.Exception("Simulated exception from pipeline");
        }
        return Task.FromResult(ContextToReturn);
    }
}
