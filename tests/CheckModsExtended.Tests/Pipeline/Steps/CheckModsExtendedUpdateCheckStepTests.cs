using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using SemanticVersioning;
using Version = SemanticVersioning.Version;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class CheckModsExtendedUpdateCheckStepTests
{
    [Fact]
    public async Task ExecuteAsync_CallsUpdateCheck()
    {
        var orchestrationService = new FakeUpdateOrchestrationService();
        var logger = new FakeLogger<CheckModsExtendedUpdateCheckStep>();
        var step = new CheckModsExtendedUpdateCheckStep(orchestrationService, logger);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            SptVersion = new Version("3.9.0")
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.True(orchestrationService.CheckForCheckModsExtendedUpdateCalled);
    }
}
