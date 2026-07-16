using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;
using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class DisplayResultsStepTests
{
    [Fact]
    public async Task ExecuteAsync_DisplaysSummary()
    {
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<DisplayResultsStep>();
        var config = new RuntimeConfig();
        var step = new DisplayResultsStep(reporter, logger, config);

        var context = new UpdateWorkflowContext 
        { 
            Args = [],
            Mods = new List<Mod> { new Mod { Status = ModStatus.Verified, Local = new LocalModIdentity { Guid = "test", FilePath = "t", IsServerMod = false, LocalName = "t", LocalAuthor = "t", LocalVersion = "1" } } }
        };

        var ex = await Record.ExceptionAsync(() => step.ExecuteAsync(context, CancellationToken.None));
        Assert.Null(ex);
    }
}
