using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class CheckModDependenciesStepTests
{
    [Fact]
    public async Task ExecuteAsync_WithDependencies_UpdatesContext()
    {
        var dependencyService = new FakeModDependencyService();
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<CheckModDependenciesStep>();
        var step = new CheckModDependenciesStep(dependencyService, reporter, logger);

        var mod = new Mod { 
            Status = ModStatus.Verified, 
            Local = new LocalModIdentity { Guid = "test", FilePath = "t", IsServerMod = false, LocalName = "t", LocalAuthor = "t", LocalVersion = "1" },
            Api = new ForgeApiMetadata { ApiModId = 123 }
        };

        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod> { mod }
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.Equal(mod.Local.Guid, context.Mods.First().Local.Guid);
    }
}
