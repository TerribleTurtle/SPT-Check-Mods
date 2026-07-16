using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class ApplyIgnoredUpdatesStepTests
{
    [Fact]
    public async Task ExecuteAsync_AppliesIgnoredUpdates_MutatesContext()
    {
        var ignoredUpdateStore = new FakeIgnoredUpdateStore();
        var mod = new Mod 
        { 
            Local = new LocalModIdentity { Guid = "test", FilePath = "test", IsServerMod = false, LocalName = "test", LocalAuthor = "test", LocalVersion = "1.0.0" },
            Update = new ModUpdateState { UpdateStatus = UpdateStatus.UpdateAvailable, LatestVersion = "2.0.0" },
            Api = new ForgeApiMetadata { ApiModId = 123 }
        };
        ignoredUpdateStore.Store = new List<IgnoredUpdate> { new IgnoredUpdate(123, "1.0.0", "2.0.0", "test", "test", IgnoreSource.User, null) };

        var orchestrationService = new UpdateOrchestrationService(
            new FakeSptInstallationService(),
            new FakeUpdateCheckService(),
            ignoredUpdateStore,
            new FakeModCheckReporter()
        );

        var logger = new FakeLogger<ApplyIgnoredUpdatesStep>();
        var step = new ApplyIgnoredUpdatesStep(orchestrationService, logger);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod> { mod }
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.True(context.Mods.First().Update.UpdateSuppressed);
        Assert.Equal(IgnoreSource.User, context.Mods.First().Update.UpdateSuppressedSource);
    }
}
