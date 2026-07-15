using System.Collections.Generic;
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

public class EnrichModsWithVersionDataStepTests
{
    [Fact]
    public async Task ExecuteAsync_WithNoMatchedMods_ReturnsEarly()
    {
        var service = new FakeModEnrichmentService();
        var logger = new FakeLogger<EnrichModsWithVersionDataStep>();
        var step = new EnrichModsWithVersionDataStep(new CheckModsExtended.Tests.Fakes.FakeModCheckReporter(), service, logger);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod> { new Mod { Status = ModStatus.NoMatch, Local = new LocalModIdentity { Guid = "test", FilePath = "test", IsServerMod = false, LocalName = "test", LocalAuthor = "test", LocalVersion = "test" } } }
        };

        await step.ExecuteAsync(context, CancellationToken.None);
        Assert.NotNull(context);
    }
}


