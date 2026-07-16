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

public class EnrichModsWithVersionDataStepTests
{
    [Fact]
    public async Task ExecuteAsync_WithMatchedMods_UpdatesContext()
    {
        var service = new FakeModEnrichmentService();
        var mod2 = new Mod { Status = ModStatus.Verified, Local = new LocalModIdentity { Guid = "test", FilePath = "t", IsServerMod = false, LocalName = "test2", LocalAuthor = "t", LocalVersion = "1" }, Api = new ForgeApiMetadata { ApiModId = 123 } };
        service.EnrichedModsToReturn = new List<Mod> { mod2 };
        var logger = new FakeLogger<EnrichModsWithVersionDataStep>();
        var step = new EnrichModsWithVersionDataStep(new FakeModCheckReporter(), service, logger);

        var mod = new Mod { Status = ModStatus.Verified, Local = new LocalModIdentity { Guid = "test", FilePath = "t", IsServerMod = false, LocalName = "t", LocalAuthor = "t", LocalVersion = "1" }, Api = new ForgeApiMetadata { ApiModId = 123 } };
        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod> { mod }
        };

        await step.ExecuteAsync(context, CancellationToken.None);
        
        Assert.Equal("test2", context.Mods.First().Local.LocalName);
    }
}
