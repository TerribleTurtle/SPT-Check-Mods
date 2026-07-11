using System;
using System.IO;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services;
using CheckMods.Services.Interfaces;
using CheckMods.Tests.Fakes;
using SemanticVersioning;
using Xunit;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Services;

public sealed class ApplicationServiceTests
{
    private readonly FakeInitializationService _initializationService = new();
    private readonly FakeSptInstallationService _sptInstallationService = new();
    private readonly FakeModScannerService _modScannerService = new();
    private readonly FakeModReconciliationService _modReconciliationService = new();
    private readonly FakeModMatchingService _modMatchingService = new();
    private readonly FakeModEnrichmentService _modEnrichmentService = new();
    private readonly FakeModDependencyService _modDependencyService = new();
    private readonly FakeIgnoredUpdateStore _ignoredUpdateStore = new();
    private readonly FakeRemoteIgnoreFileClient _remoteIgnoreFileClient = new();
    private readonly FakeModCheckReporter _reporter = new();
    private readonly FakeLogger<ApplicationService> _logger = new();
    private readonly FakeModResolutionService _modResolutionService = new();
    private readonly FakeCompatibilityValidationService _compatibilityValidationService = new();
    private readonly FakeUpdateOrchestrationService _updateOrchestrationService = new();

    private readonly ApplicationService _sut;

    public ApplicationServiceTests()
    {
        _sut = new ApplicationService(
            _initializationService,
            _sptInstallationService,
            _modScannerService,
            _modReconciliationService,
            _modMatchingService,
            _modEnrichmentService,
            _modDependencyService,
            _ignoredUpdateStore,
            _remoteIgnoreFileClient,
            _reporter,
            _logger,
            _modResolutionService,
            _compatibilityValidationService,
            _updateOrchestrationService
        );
    }

    [Fact]
    public async Task run_async_given_invalid_path_returns_empty_and_short_circuits()
    {
        // Act
        var result = await _sut.RunAsync([Path.Combine("invalid", "path", "that", "does", "not", "exist")]);

        // Assert
        Assert.Empty(result);
        Assert.True(_initializationService.GetValidatedSptPathCalled);
        Assert.False(_modEnrichmentService.WasCalled);
    }

    [Fact]
    public async Task run_async_when_dependency_throws_exception_handles_gracefully()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var sptVersion = new Version("3.9.0");
        _sptInstallationService.ValidatedVersion = sptVersion;

        var serverMod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
            {
                Guid = "test",
                FilePath = "test",
                LocalName = "TestMod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
        };
        _modScannerService.ServerModsToReturn = [serverMod];

        _modReconciliationService.ResultToReturn = new ModReconciliationResult
        {
            Mods = [serverMod],
            ReconciledPairs = [],
            UnmatchedServerMods = [serverMod],
            UnmatchedClientMods = [],
        };

        _modMatchingService.MatchModAction = _ => throw new InvalidOperationException("Simulated failure");

        // Act
        var result = await _sut.RunAsync([currentDir]);

        // Assert
        Assert.Empty(result);
        Assert.NotNull(_reporter.LastException);
        Assert.IsType<InvalidOperationException>(_reporter.LastException);
    }

    [Fact]
    public async Task run_async_happy_path_executes_core_components_in_correct_order()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        _sptInstallationService.ValidatedVersion = new Version("3.9.0");

        var apiResult = new ModSearchResult(
            Id: 1,
            HubId: null,
            Name: "Test Mod",
            Slug: "test-mod",
            Teaser: null,
            Thumbnail: null,
            Downloads: 0,
            SourceCodeLinks: null,
            DetailUrl: "url",
            Owner: new ModAuthor(1, "Author", null),
            Versions: []
        );

        var mod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
            {
                Guid = "test",
                FilePath = "test",
                LocalName = "TestMod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
            LoadWarnings = ["Warning!"],
        };
        mod = mod.WithApiMatch(apiResult);

        _modScannerService.ServerModsToReturn = [mod];

        _modReconciliationService.ResultToReturn = new ModReconciliationResult
        {
            Mods = [mod],
            ReconciledPairs = [],
            UnmatchedServerMods = [mod],
            UnmatchedClientMods = [],
        };

        // Act
        var result = await _sut.RunAsync([currentDir]);

        // Assert
        Assert.Single(result);
        Assert.True(_initializationService.GetValidatedSptPathCalled);
        Assert.True(_initializationService.RemoveLegacyApiKeyFileCalled);
        Assert.True(_updateOrchestrationService.CheckForCheckModsUpdateCalled);
        Assert.True(_modResolutionService.FetchSourceCodeUrlsForModsCalled);
        Assert.True(_modEnrichmentService.WasCalled);
        Assert.True(_updateOrchestrationService.ApplyIgnoredUpdatesCalled);
        Assert.True(_compatibilityValidationService.CheckModVersionCompatibilityCalled);
        Assert.Contains(_reporter.Headings, h => h.Contains("Verifying Forge records"));
    }

    [Fact]
    public async Task run_async_mutates_state_when_subservices_return_updated_immutable_records()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        _sptInstallationService.ValidatedVersion = new Version("3.9.0");

        var mod = new Mod
        {
            Local = new CheckMods.Models.LocalModIdentity
            {
                Guid = "test-guid",
                FilePath = "test",
                LocalName = "TestMod",
                LocalAuthor = "Author",
                LocalVersion = "1.0.0",
                IsServerMod = true,
            },
        };

        // Scanner finds the initial mod
        _modScannerService.ServerModsToReturn = [mod];

        // Reconciliation pairs it
        _modReconciliationService.ResultToReturn = new ModReconciliationResult
        {
            Mods = [mod],
            ReconciledPairs = [],
            UnmatchedServerMods = [mod],
            UnmatchedClientMods = [],
        };

        // The enrichment service will return a mutated mod (e.g. with ApiModId)
        var enrichedMod = mod.WithApiMatch(new ModSearchResult(
            99, null, "Test", "test", null, null, 0, null, null, new ModAuthor(1, "A", null), []
        ));
        _modEnrichmentService.EnrichedModsToReturn = [enrichedMod];

        // The update orchestration service will return the mod mutated again (e.g. Ignored)
        var ignoredMod = enrichedMod.WithUpdateSuppressed(true);
        _updateOrchestrationService.ModsToReturn = [ignoredMod];

        // Act
        var result = await _sut.RunAsync([currentDir]);

        // Assert
        var finalMod = Assert.Single(result);
        
        // Ensure the final returned list contains the fully mutated mod
        Assert.Equal(99, finalMod.Api.ApiModId);
        Assert.True(finalMod.Update.UpdateSuppressed);
    }
}
