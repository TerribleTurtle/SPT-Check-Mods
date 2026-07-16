using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fixtures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class ModReconciliationServiceTests
{
    private readonly ModReconciliationService _service;

    public ModReconciliationServiceTests()
    {
        _service = new ModReconciliationService(NullLogger<ModReconciliationService>.Instance);
    }

    [Fact]
    public void ReconcileMods_matches_by_guid()
    {
        var serverMods = new List<Mod> { ModFixture.CreateServerMod("com.author.mod") };
        var clientMods = new List<Mod> { ModFixture.CreateClientMod("com.author.mod") };

        var result = _service.ReconcileMods(serverMods, clientMods);

        Assert.Single(result.ReconciledPairs);
        Assert.Empty(result.UnmatchedServerMods);
        Assert.Empty(result.UnmatchedClientMods);
    }

    [Fact]
    public void ReconcileMods_matches_by_normalized_name_when_guid_missing()
    {
        var serverMod = ModFixture.CreateServerMod("", name: "SomeMod-Server");
        var clientMod = ModFixture.CreateClientMod("", name: "SomeMod-Client");

        var serverMods = new List<Mod> { serverMod };
        var clientMods = new List<Mod> { clientMod };

        var result = _service.ReconcileMods(serverMods, clientMods);

        Assert.Single(result.ReconciledPairs);
        Assert.Empty(result.UnmatchedServerMods);
        Assert.Empty(result.UnmatchedClientMods);
    }

    [Fact]
    public void ReconcileMods_selects_newer_version()
    {
        var serverMod = ModFixture.CreateServerMod("com.test", version: "1.0.0");
        var clientMod = ModFixture.CreateClientMod("com.test", version: "1.1.0");

        var result = _service.ReconcileMods(new List<Mod> { serverMod }, new List<Mod> { clientMod });

        Assert.Single(result.ReconciledPairs);
        var pair = result.ReconciledPairs[0];
        Assert.Equal("1.1.0", pair.SelectedMod.Local.LocalVersion);
        Assert.False(pair.SelectedMod.Local.IsServerMod);
    }

    [Fact]
    public void ReconcileMods_falls_back_to_server_mod_when_versions_equal()
    {
        var serverMod = ModFixture.CreateServerMod("com.test", version: "1.0.0");
        var clientMod = ModFixture.CreateClientMod("com.test", version: "1.0.0");

        var result = _service.ReconcileMods(new List<Mod> { serverMod }, new List<Mod> { clientMod });

        Assert.Single(result.ReconciledPairs);
        var pair = result.ReconciledPairs[0];
        Assert.Equal("1.0.0", pair.SelectedMod.Local.LocalVersion);
        Assert.True(pair.SelectedMod.Local.IsServerMod);
    }

    [Fact]
    public void ReconcileMods_handles_unmatched_mods()
    {
        var serverMod = ModFixture.CreateServerMod("com.test.server", name: "ServerModUnmatched");
        var clientMod = ModFixture.CreateClientMod("com.test.client", name: "ClientModUnmatched");

        var result = _service.ReconcileMods(new List<Mod> { serverMod }, new List<Mod> { clientMod });

        Assert.Empty(result.ReconciledPairs);
        Assert.Single(result.UnmatchedServerMods);
        Assert.Single(result.UnmatchedClientMods);
        Assert.Equal(2, result.Mods.Count);
    }
}
