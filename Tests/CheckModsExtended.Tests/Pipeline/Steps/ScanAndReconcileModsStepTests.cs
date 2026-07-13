using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class ScanAndReconcileModsStepTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoMods_CancelsContext()
    {
        var modScannerService = new FakeModScannerService();
        var modResolutionService = new FakeModResolutionService();
        var modReconciliationService = new FakeModReconciliationService();
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<ScanAndReconcileModsStep>();

        var step = new ScanAndReconcileModsStep(
            modScannerService,
            modResolutionService,
            modReconciliationService,
            reporter,
            logger,
            new CheckModsExtended.Services.MisplacedModAnalyzerService()
        );

        var context = new UpdateWorkflowContext
        {
            Args = [],
            SptPath = "C:\\SPT"
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.True(context.IsCancelled);
    }
}
