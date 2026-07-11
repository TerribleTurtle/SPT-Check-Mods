using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models.Pipeline;
using CheckMods.Services.Pipeline;
using CheckMods.Tests.Fakes;
using CheckMods.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckMods.Tests.Services.Pipeline;

public sealed class UpdateWorkflowOrchestratorTests
{
    [Fact]
    public async Task Run_pipeline_async_executes_all_steps_in_order_and_returns_mods()
    {
        // Arrange
        var step1 = new FakeWorkflowStep();
        var step2 = new FakeWorkflowStep
        {
            ExecuteAction = context => context.Mods.Add(ModFixture.CreateClientMod("test-guid"))
        };

        var orchestrator = new UpdateWorkflowOrchestrator(
            new[] { step1, step2 },
            NullLogger<UpdateWorkflowOrchestrator>.Instance);

        var args = Array.Empty<string>();

        // Act
        var result = await orchestrator.RunPipelineAsync(args, CancellationToken.None);

        // Assert
        Assert.True(step1.WasExecuted);
        Assert.True(step2.WasExecuted);
        Assert.Single(result);
        Assert.Equal("test-guid", result[0].Local.Guid);
    }

    [Fact]
    public async Task Run_pipeline_async_short_circuits_if_context_is_cancelled()
    {
        // Arrange
        var step1 = new FakeWorkflowStep
        {
            ExecuteAction = context => context.IsCancelled = true
        };
        var step2 = new FakeWorkflowStep();

        var orchestrator = new UpdateWorkflowOrchestrator(
            new[] { step1, step2 },
            NullLogger<UpdateWorkflowOrchestrator>.Instance);

        var args = Array.Empty<string>();

        // Act
        var result = await orchestrator.RunPipelineAsync(args, CancellationToken.None);

        // Assert
        Assert.True(step1.WasExecuted);
        Assert.False(step2.WasExecuted);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Run_pipeline_async_bubbles_up_exceptions()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var step1 = new FakeWorkflowStep
        {
            ExecuteAction = context => throw expectedException
        };
        var step2 = new FakeWorkflowStep();

        var orchestrator = new UpdateWorkflowOrchestrator(
            new[] { step1, step2 },
            NullLogger<UpdateWorkflowOrchestrator>.Instance);

        var args = Array.Empty<string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.RunPipelineAsync(args, CancellationToken.None));
            
        Assert.Same(expectedException, exception);
        Assert.True(step1.WasExecuted);
        Assert.False(step2.WasExecuted);
    }
}
