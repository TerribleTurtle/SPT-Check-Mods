using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class DetectMisplacedModsStepTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesContextWithMisplacedReport()
    {
        var modScannerService = new FakeModScannerService();
        var report = new MisplacedModReport(new List<MisplacedMod>(), new List<CrossInstalledDirectory>());
        modScannerService.MisplacedModReportToReturn = report;
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<DetectMisplacedModsStep>();
        var step = new DetectMisplacedModsStep(modScannerService, reporter, logger);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            SptPath = "C:\\SPT"
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(context.MisplacedReport);
        Assert.Same(report, context.MisplacedReport);
    }
}
