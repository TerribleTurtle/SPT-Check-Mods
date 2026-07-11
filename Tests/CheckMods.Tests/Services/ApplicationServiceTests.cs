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
    private readonly FakeForgeApiService _forgeApiService = new();
    private readonly FakeSptInstallationService _sptInstallationService = new();
    private readonly FakeModScannerService _modScannerService = new();
    private readonly FakeModReconciliationService _modReconciliationService = new();
    private readonly FakeModMatchingService _modMatchingService = new();
    private readonly FakeModEnrichmentService _modEnrichmentService = new();
    private readonly FakeModDependencyService _modDependencyService = new();
    private readonly FakeUpdateCheckService _updateCheckService = new();
    private readonly FakeIgnoredUpdateStore _ignoredUpdateStore = new();
    private readonly FakeRemoteIgnoreFileClient _remoteIgnoreFileClient = new();
    private readonly FakeModCheckReporter _reporter = new();
    private readonly FakeLogger<ApplicationService> _logger = new();

    private readonly ApplicationService _sut;

    public ApplicationServiceTests()
    {
        _sut = new ApplicationService(
            _initializationService,
            _forgeApiService,
            _sptInstallationService,
            _modScannerService,
            _modReconciliationService,
            _modMatchingService,
            _modEnrichmentService,
            _modDependencyService,
            _updateCheckService,
            _ignoredUpdateStore,
            _remoteIgnoreFileClient,
            _reporter,
            _logger
        );
    }

    [Fact]
    public async Task Run_async_given_invalid_path_returns_empty_and_short_circuits()
    {
        // Act
        var result = await _sut.RunAsync([Path.Combine("invalid", "path", "that", "does", "not", "exist")]);

        // Assert
        Assert.Empty(result);
        Assert.True(_initializationService.GetValidatedSptPathCalled);
        Assert.False(_modEnrichmentService.WasCalled);
    }

    [Fact]
    public async Task Run_async_when_dependency_throws_exception_handles_gracefully()
    {
        // Arrange
        var currentDir = Directory.GetCurrentDirectory();
        var sptVersion = new Version("3.9.0");
        _sptInstallationService.ValidatedVersion = sptVersion;
        
        var serverMod = new Mod { Guid = "test", FilePath = "test", LocalName = "TestMod", LocalAuthor = "Author", LocalVersion = "1.0.0", IsServerMod = true };
        _modScannerService.ServerModsToReturn = [serverMod];
        
        _modReconciliationService.ResultToReturn = new ModReconciliationResult
        {
            Mods = [serverMod],
            ReconciledPairs = [],
            UnmatchedServerMods = [serverMod],
            UnmatchedClientMods = []
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
    public async Task Run_async_with_ignored_updates_suppresses_update_status()
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
            Guid = "test",
            FilePath = "test", 
            LocalName = "TestMod", 
            LocalAuthor = "Author",
            LocalVersion = "1.0.0",
            IsServerMod = true
        };
        mod.UpdateFromApiMatch(apiResult);
        
        var updateVersion = new ModUpdateVersion(null, 1, "test", "Test Mod", "test-mod", "2.0.0", "url", null);
        mod.UpdateFromSafeToUpdate(new SafeToUpdateMod(null, updateVersion, null));

        _modScannerService.ServerModsToReturn = [mod];
        
        _modReconciliationService.ResultToReturn = new ModReconciliationResult
        {
            Mods = [mod],
            ReconciledPairs = [],
            UnmatchedServerMods = [mod],
            UnmatchedClientMods = []
        };

        _ignoredUpdateStore.Store = [new IgnoredUpdate(1, "1.0.0", "2.0.0")];

        // Act
        var result = await _sut.RunAsync([currentDir]);

        // Assert
        Assert.Single(result);
        var returnedMod = result[0];
        Assert.True(returnedMod.UpdateSuppressed);
    }

    [Fact]
    public async Task Run_async_happy_path_executes_core_components_in_correct_order()
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
            Guid = "test",
            FilePath = "test", 
            LocalName = "TestMod", 
            LocalAuthor = "Author",
            LocalVersion = "1.0.0",
            IsServerMod = true
        };
        mod.UpdateFromApiMatch(apiResult);

        _modScannerService.ServerModsToReturn = [mod];
        
        _modReconciliationService.ResultToReturn = new ModReconciliationResult
        {
            Mods = [mod],
            ReconciledPairs = [],
            UnmatchedServerMods = [mod],
            UnmatchedClientMods = []
        };

        // Act
        var result = await _sut.RunAsync([currentDir]);

        // Assert
        Assert.Single(result);
        Assert.True(_initializationService.GetValidatedSptPathCalled);
        Assert.True(_initializationService.RemoveLegacyApiKeyFileCalled);
        Assert.True(_modEnrichmentService.WasCalled);
        Assert.Contains(_reporter.Headings, h => h.Contains("Verifying Forge records"));
    }
}
