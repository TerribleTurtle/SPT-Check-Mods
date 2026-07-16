using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using SemanticVersioning;
using Version = SemanticVersioning.Version;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class MatchModsWithApiStepTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesContextMods()
    {
        var service = new FakeModMatchingService();
        service.MatchModAction = m => m with { Status = ModStatus.Verified };
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<MatchModsWithApiStep>();
        var step = new MatchModsWithApiStep(service, reporter, logger);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod> { new Mod { Local = new LocalModIdentity { Guid = "test", FilePath = "t", IsServerMod = false, LocalName = "t", LocalAuthor = "t", LocalVersion = "1" } } },
            SptVersion = new Version("3.9.0")
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal(ModStatus.Verified, context.Mods.First().Status);
    }
}
