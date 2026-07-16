using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class ScanAndReconcileModsStepTests
{
    [Fact]
    public async Task ExecuteAsync_WithMods_PopulatesContextMods()
    {
        var modScannerService = new FakeModScannerService();
        var modResolutionService = new FakeModResolutionService();
        var modReconciliationService = new FakeModReconciliationService();
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<ScanAndReconcileModsStep>();

        var returnedMods = new List<Mod> { new Mod { Local = new LocalModIdentity { Guid = "test", FilePath = "t", IsServerMod = false, LocalName = "t", LocalAuthor = "t", LocalVersion = "1" } } };
        modScannerService.ServerModsToReturn = returnedMods;
        modReconciliationService.ResultToReturn = new CheckModsExtended.Services.Interfaces.ModReconciliationResult { Mods = returnedMods, ReconciledPairs = [], UnmatchedServerMods = [], UnmatchedClientMods = [] };

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

        Assert.False(context.IsCancelled);
        Assert.Equal(returnedMods.First().Local.Guid, context.Mods.First().Local.Guid);
    }
}
