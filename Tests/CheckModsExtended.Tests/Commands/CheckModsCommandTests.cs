using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class CheckModsCommandTests
{
    private Task<int> RunExecuteAsync(CheckModsCommand command, CommandContext context, CheckModsCommand.Settings settings, CancellationToken cancellationToken)
    {
        var method = typeof(CheckModsCommand).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        return (Task<int>)method.Invoke(command, new object[] { context, settings, cancellationToken })!;
    }

    [Fact]
    public async Task ExecuteAsync_WithSptPath_PassesArgsToOrchestrator()
    {
        // Arrange
        var fakeOrchestrator = new FakeUpdateWorkflowOrchestrator();
        var fakeWorkflow = new FakeIgnoredUpdateWorkflow { ChoiceToReturn = EndOfRunChoice.Exit };
        var fakeScanCache = new FakePluginScanCache();
        var fakeCacheManager = new FakeCacheManager();

        var command = new CheckModsCommand(fakeOrchestrator, fakeWorkflow, fakeScanCache, fakeCacheManager);
        var settings = new CheckModsCommand.Settings { SptPath = "test/path" };
        
        // Act
        var exitCode = await RunExecuteAsync(command, null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(ExitCodes.ExitRequested, exitCode);
    }

    [Fact]
    public async Task ExecuteAsync_RescanChoice_ClearsCachesAndLoops()
    {
        // Arrange
        var fakeOrchestrator = new FakeUpdateWorkflowOrchestrator();
        fakeOrchestrator.ContextToReturn = new UpdateWorkflowContext 
        { 
            Args = Array.Empty<string>(),
            Mods = new List<Mod>()
        };

        var fakeScanCache = new FakePluginScanCache();
        var fakeCacheManager = new FakeCacheManager();
        var customWorkflow = new StatefulFakeWorkflow();

        var command = new CheckModsCommand(fakeOrchestrator, customWorkflow, fakeScanCache, fakeCacheManager);
        var settings = new CheckModsCommand.Settings();
        
        // Act
        var exitCode = await RunExecuteAsync(command, null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(2, customWorkflow.CallCount);
        Assert.True(fakeScanCache.ClearCallCount > 0);
        Assert.True(fakeCacheManager.ClearCallCount > 0);
    }

    [Fact]
    public async Task ExecuteAsync_LaunchWebGui_ReturnsWebGuiExitCode()
    {
        // Arrange
        var fakeOrchestrator = new FakeUpdateWorkflowOrchestrator();
        fakeOrchestrator.ContextToReturn = new UpdateWorkflowContext 
        { 
            Args = Array.Empty<string>(),
            Mods = new List<Mod>()
        };

        var fakeWorkflow = new FakeIgnoredUpdateWorkflow { ChoiceToReturn = EndOfRunChoice.LaunchWebGui };
        var fakeScanCache = new FakePluginScanCache();
        var fakeCacheManager = new FakeCacheManager();

        var command = new CheckModsCommand(fakeOrchestrator, fakeWorkflow, fakeScanCache, fakeCacheManager);
        var settings = new CheckModsCommand.Settings();
        
        // Act
        var exitCode = await RunExecuteAsync(command, null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(ExitCodes.LaunchWebGui, exitCode);
    }

    private sealed class StatefulFakeWorkflow : CheckModsExtended.Services.Interfaces.IIgnoredUpdateWorkflow
    {
        public int CallCount { get; private set; }

        public Task<EndOfRunChoice> RunAsync(IReadOnlyList<Mod>? mods, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (CallCount == 1)
                return Task.FromResult(EndOfRunChoice.Rescan);
            
            return Task.FromResult(EndOfRunChoice.Exit);
        }
    }
}
