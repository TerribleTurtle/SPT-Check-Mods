using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services;
using CheckMods.Tests.Fakes;
using SemanticVersioning;
using Xunit;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Services;

public sealed class UpdateOrchestrationServiceTests
{
    private readonly FakeSptInstallationService _sptInstallationService = new();
    private readonly FakeUpdateCheckService _updateCheckService = new();
    private readonly FakeIgnoredUpdateStore _ignoredUpdateStore = new();
    private readonly FakeModCheckReporter _reporter = new();

    private readonly UpdateOrchestrationService _sut;

    public UpdateOrchestrationServiceTests()
    {
        _sut = new UpdateOrchestrationService(
            _sptInstallationService,
            _updateCheckService,
            _ignoredUpdateStore,
            _reporter
        );
    }

    [Fact]
    public async Task applyignoredupdates_suppresses_update_when_ignored()
    {
        // Arrange
        var mod = new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = "test",
                FilePath = "test",
                LocalName = "Test Mod",
                LocalAuthor = "Author",
                IsServerMod = true,
                LocalVersion = "1.0.0",
            },
        };

        var apiResult = new ModSearchResult(
            1,
            null,
            "Test Mod",
            "test-mod",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            []
        );
        mod = mod.WithApiMatch(apiResult);

        var updateVersion = new ModUpdateVersion(null, 1, "test", "Test Mod", "test-mod", "2.0.0", "url", null);
        mod = mod.WithSafeToUpdate(new SafeToUpdateMod(null, updateVersion, null));

        _ignoredUpdateStore.Store = [new IgnoredUpdate(1, "1.0.0", "2.0.0")];

        // Act
        var result = await _sut.ApplyIgnoredUpdatesAsync([mod]);

        // Assert
        Assert.True(result[0].Update.UpdateSuppressed);
    }
}
