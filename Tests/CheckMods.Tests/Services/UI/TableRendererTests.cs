using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Services.UI;
using Xunit;

namespace CheckMods.Tests.Services.UI;

/// <summary>
/// Tests for <see cref="TableRenderer"/> to ensure it acts as a proper facade.
/// </summary>
public sealed class TableRendererTests
{
    private sealed class FakeVersionTableUiRenderer : IVersionTableUiRenderer
    {
        public bool VersionTableCalled { get; private set; }
        public bool VersionCompatibilityResultsCalled { get; private set; }

        public void VersionTable(List<Mod> mods)
        {
            VersionTableCalled = true;
        }

        public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion)
        {
            VersionCompatibilityResultsCalled = true;
        }
    }

    private sealed class FakeReconciliationUiRenderer : IReconciliationUiRenderer
    {
        public bool ReconciliationResultsCalled { get; private set; }
        public bool LoadingWarningsCalled { get; private set; }
        public bool UnverifiedModsCalled { get; private set; }

        public void ReconciliationResults(ModReconciliationResult result)
        {
            ReconciliationResultsCalled = true;
        }

        public void LoadingWarnings(List<Mod> modsWithWarnings)
        {
            LoadingWarningsCalled = true;
        }

        public void UnverifiedMods(List<Mod> mods)
        {
            UnverifiedModsCalled = true;
        }
    }

    private sealed class FakeMisplacedModUiRenderer : IMisplacedModUiRenderer
    {
        public bool MisplacedModsCalled { get; private set; }

        public void MisplacedMods(MisplacedModReport report)
        {
            MisplacedModsCalled = true;
        }
    }

    private sealed class FakeDependencyUiRenderer : IDependencyUiRenderer
    {
        public bool DependencyResultsCalled { get; private set; }

        public void DependencyResults(DependencyAnalysisResult result)
        {
            DependencyResultsCalled = true;
        }
    }

    [Fact]
    public void version_compatibility_results_delegates_to_version_table_renderer()
    {
        var fakeVersion = new FakeVersionTableUiRenderer();
        var facade = new TableRenderer(
            fakeVersion,
            new FakeReconciliationUiRenderer(),
            new FakeMisplacedModUiRenderer(),
            new FakeDependencyUiRenderer()
        );

        facade.VersionCompatibilityResults(new List<Mod>(), new SemanticVersioning.Version(0, 0, 0));

        Assert.True(fakeVersion.VersionCompatibilityResultsCalled);
    }

    [Fact]
    public void loading_warnings_delegates_to_reconciliation_renderer()
    {
        var fakeReconciliation = new FakeReconciliationUiRenderer();
        var facade = new TableRenderer(
            new FakeVersionTableUiRenderer(),
            fakeReconciliation,
            new FakeMisplacedModUiRenderer(),
            new FakeDependencyUiRenderer()
        );

        facade.LoadingWarnings(new List<Mod>());

        Assert.True(fakeReconciliation.LoadingWarningsCalled);
    }

    [Fact]
    public void reconciliation_results_delegates_to_reconciliation_renderer()
    {
        var fakeReconciliation = new FakeReconciliationUiRenderer();
        var facade = new TableRenderer(
            new FakeVersionTableUiRenderer(),
            fakeReconciliation,
            new FakeMisplacedModUiRenderer(),
            new FakeDependencyUiRenderer()
        );

        facade.ReconciliationResults(
            new ModReconciliationResult
            {
                Mods = new List<Mod>(),
                ReconciledPairs = new List<ModPair>(),
                UnmatchedServerMods = new List<Mod>(),
                UnmatchedClientMods = new List<Mod>(),
            }
        );

        Assert.True(fakeReconciliation.ReconciliationResultsCalled);
    }

    [Fact]
    public void misplaced_mods_delegates_to_misplaced_mod_renderer()
    {
        var fakeMisplaced = new FakeMisplacedModUiRenderer();
        var facade = new TableRenderer(
            new FakeVersionTableUiRenderer(),
            new FakeReconciliationUiRenderer(),
            fakeMisplaced,
            new FakeDependencyUiRenderer()
        );

        facade.MisplacedMods(new MisplacedModReport(new List<MisplacedMod>(), new List<CrossInstalledDirectory>()));

        Assert.True(fakeMisplaced.MisplacedModsCalled);
    }

    [Fact]
    public void unverified_mods_delegates_to_reconciliation_renderer()
    {
        var fakeReconciliation = new FakeReconciliationUiRenderer();
        var facade = new TableRenderer(
            new FakeVersionTableUiRenderer(),
            fakeReconciliation,
            new FakeMisplacedModUiRenderer(),
            new FakeDependencyUiRenderer()
        );

        facade.UnverifiedMods(new List<Mod>());

        Assert.True(fakeReconciliation.UnverifiedModsCalled);
    }

    [Fact]
    public void dependency_results_delegates_to_dependency_renderer()
    {
        var fakeDependency = new FakeDependencyUiRenderer();
        var facade = new TableRenderer(
            new FakeVersionTableUiRenderer(),
            new FakeReconciliationUiRenderer(),
            new FakeMisplacedModUiRenderer(),
            fakeDependency
        );

        facade.DependencyResults(
            new DependencyAnalysisResult
            {
                RootMods = new List<DependencyNode>(),
                Conflicts = new List<DependencyConflict>(),
                MissingDependencies = new List<MissingDependency>(),
            }
        );

        Assert.True(fakeDependency.DependencyResultsCalled);
    }

    [Fact]
    public void version_table_delegates_to_version_table_renderer()
    {
        var fakeVersion = new FakeVersionTableUiRenderer();
        var facade = new TableRenderer(
            fakeVersion,
            new FakeReconciliationUiRenderer(),
            new FakeMisplacedModUiRenderer(),
            new FakeDependencyUiRenderer()
        );

        facade.VersionTable(new List<Mod>());

        Assert.True(fakeVersion.VersionTableCalled);
    }
}
