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

public class CheckModVersionCompatibilityStepTests
{
    [Fact]
    public async Task ExecuteAsync_UpdatesContextWithCompatibilityData()
    {
        var validationService = new FakeCompatibilityValidationService();
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<CheckModVersionCompatibilityStep>();
        var step = new CheckModVersionCompatibilityStep(validationService, reporter, logger);

        var mod = new Mod { Local = new LocalModIdentity { Guid = "test", FilePath = "t", IsServerMod = false, LocalName = "t", LocalAuthor = "t", LocalVersion = "1" } };
        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod> { mod },
            SptVersion = new Version("3.9.0")
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.NotNull(context.Mods);
        Assert.Single(context.Mods);
    }
}
