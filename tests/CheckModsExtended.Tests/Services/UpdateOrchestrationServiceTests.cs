using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using CheckModsExtended.Tests.Fixtures;
using SemanticVersioning;
using Xunit;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Services;

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
    public async Task Applyignoredupdates_suppresses_update_when_ignored()
    {
        var mod = CreateModWithUpdateAvailable();
        _ignoredUpdateStore.Store = [new IgnoredUpdate(1, "1.0.0", "2.0.0")];

        var result = await _sut.ApplyIgnoredUpdatesAsync([mod]);

        Assert.True(result[0].Update.UpdateSuppressed);
    }

    [Fact]
    public async Task ApplyIgnoredUpdates_DoesNotSuppress_WhenNotIgnored()
    {
        var mod = CreateModWithUpdateAvailable();
        _ignoredUpdateStore.Store = []; // Empty store

        var result = await _sut.ApplyIgnoredUpdatesAsync([mod]);

        Assert.False(result[0].Update.UpdateSuppressed);
    }

    [Fact]
    public async Task CheckForSptUpdatesAsync_LogsSuccess_WhenNoUpdates()
    {
        _sptInstallationService.Updates = [];
        
        await _sut.CheckForSptUpdatesAsync(new Version("1.0.0"));
        
        Assert.Contains("You are running the latest version of SPT!", _reporter.Successes);
    }

    [Fact]
    public async Task CheckForCheckModsExtendedUpdateAsync_Works()
    {
        _updateCheckService.ResultToReturn = new CheckModsExtendedUpdateResult(CheckModsExtendedUpdateStatus.UpdateAvailable, "2.0.0");
        
        await _sut.CheckForCheckModsExtendedUpdateAsync(new Version("1.0.0"));
        
        Assert.Contains("Checking for Check Mods updates...", _reporter.Headings);
    }

    private static Mod CreateModWithUpdateAvailable()
    {
        var mod = ModFixture.CreateServerMod("test", "Test Mod");

        var apiResult = new ModSearchResult(
            1, null, "Test Mod", "test-mod", null, null, 0, null, "url",
            new ModAuthor(1, "Author", null), []
        );
        mod = mod.WithApiMatch(apiResult);

        var updateVersion = new ModUpdateVersion(null, 1, "test", "Test Mod", "test-mod", "2.0.0", "url", null);
        mod = mod.WithSafeToUpdate(new SafeToUpdateMod(null, updateVersion, null));
        
        return mod;
    }
}
