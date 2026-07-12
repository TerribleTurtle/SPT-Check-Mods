using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline;
using CheckModsExtended.Tests.Fakes;
using CheckModsExtended.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckModsExtended.Tests.Services.Pipeline;

public sealed class UpdateWorkflowOrchestratorTests
{
    [Fact]
    public async Task Run_pipeline_async_executes_all_steps_in_order_and_returns_mods()
    {
        // Arrange
        var step1 = new FakeWorkflowStep();
        var step2 = new FakeWorkflowStep
        {
            ExecuteAction = context => context.Mods.Add(ModFixture.CreateClientMod("test-guid")),
        };

        var orchestrator = new UpdateWorkflowOrchestrator(
            [step1, step2],
            NullLogger<UpdateWorkflowOrchestrator>.Instance
        );

        string[] args = [];

        // Act
        var result = await orchestrator.RunPipelineAsync(args, CancellationToken.None);

        // Assert
        Assert.True(step1.WasExecuted);
        Assert.True(step2.WasExecuted);
        Assert.Single(result.Mods);
        Assert.Equal("test-guid", result.Mods[0].Local.Guid);
    }

    [Fact]
    public async Task Run_pipeline_async_short_circuits_if_context_is_cancelled()
    {
        // Arrange
        var step1 = new FakeWorkflowStep { ExecuteAction = context => context.IsCancelled = true };
        var step2 = new FakeWorkflowStep();

        var orchestrator = new UpdateWorkflowOrchestrator(
            [step1, step2],
            NullLogger<UpdateWorkflowOrchestrator>.Instance
        );

        string[] args = [];

        // Act
        var result = await orchestrator.RunPipelineAsync(args, CancellationToken.None);

        // Assert
        Assert.True(step1.WasExecuted);
        Assert.False(step2.WasExecuted);
        Assert.Empty(result.Mods);
    }

    [Fact]
    public async Task Run_pipeline_async_bubbles_up_exceptions()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var step1 = new FakeWorkflowStep { ExecuteAction = context => throw expectedException };
        var step2 = new FakeWorkflowStep();

        var orchestrator = new UpdateWorkflowOrchestrator(
            [step1, step2],
            NullLogger<UpdateWorkflowOrchestrator>.Instance
        );

        string[] args = [];

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.RunPipelineAsync(args, CancellationToken.None)
        );

        Assert.Same(expectedException, exception);
        Assert.True(step1.WasExecuted);
        Assert.False(step2.WasExecuted);
    }
}
