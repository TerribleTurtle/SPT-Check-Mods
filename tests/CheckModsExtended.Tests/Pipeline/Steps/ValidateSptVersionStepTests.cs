using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using SemanticVersioning;
using Version = SemanticVersioning.Version;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class ValidateSptVersionStepTests
{
    [Fact]
    public async Task ExecuteAsync_WhenVersionIsValid_SetsVersionAndChecksForUpdates()
    {
        var installationService = new FakeSptInstallationService();
        installationService.ValidatedVersion = new Version("3.9.0");
        var reporter = new FakeModCheckReporter();
        
        var orchestrationService = new UpdateOrchestrationService(
            installationService,
            new FakeUpdateCheckService(),
            new FakeIgnoredUpdateStore(),
            reporter
        );
        var logger = new FakeLogger<ValidateSptVersionStep>();

        var step = new ValidateSptVersionStep(installationService, orchestrationService, reporter, logger);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            SptPath = "C:\\SPT"
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.False(context.IsCancelled);
        Assert.Equal(new Version("3.9.0"), context.SptVersion);
        Assert.Contains(reporter.Statuses, h => h.Contains("Checking for SPT updates"));
    }
}
