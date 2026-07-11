using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.UI;

namespace CheckModsExtended.Services;

public sealed class SpectreModCheckReporter : IModCheckReporter
{
    private readonly ITextRenderer _textRenderer;
    private readonly IVersionTableUiRenderer _versionTableRenderer;
    private readonly IReconciliationUiRenderer _reconciliationRenderer;
    private readonly IMisplacedModUiRenderer _misplacedModRenderer;
    private readonly IDependencyUiRenderer _dependencyRenderer;
    private readonly IMiscTableUiRenderer _miscTableRenderer;
    private readonly IUserPromptService _promptService;
    private readonly IProgressRenderer _progressRenderer;

    public SpectreModCheckReporter(
        ITextRenderer textRenderer,
        IVersionTableUiRenderer versionTableRenderer,
        IReconciliationUiRenderer reconciliationRenderer,
        IMisplacedModUiRenderer misplacedModRenderer,
        IDependencyUiRenderer dependencyRenderer,
        IMiscTableUiRenderer miscTableRenderer,
        IUserPromptService promptService,
        IProgressRenderer progressRenderer
    )
    {
        _textRenderer = textRenderer;
        _versionTableRenderer = versionTableRenderer;
        _reconciliationRenderer = reconciliationRenderer;
        _misplacedModRenderer = misplacedModRenderer;
        _dependencyRenderer = dependencyRenderer;
        _miscTableRenderer = miscTableRenderer;
        _promptService = promptService;
        _progressRenderer = progressRenderer;
    }

    public void Banner() { _textRenderer.Banner(); }
    public void Rule() { _textRenderer.Rule(); }
    public void Blank() { _textRenderer.Blank(); }
    public void Heading(string text) { _textRenderer.Heading(text); }
    public void Status(string text) { _textRenderer.Status(text); }
    public void Success(string text) { _textRenderer.Success(text); }
    public void Warning(string text) { _textRenderer.Warning(text); }
    public void Error(string text) { _textRenderer.Error(text); }
    public void Exception(Exception ex) { _textRenderer.Exception(ex); }
    public void CouldNotReadModDll(string fileName, string reason) { _textRenderer.CouldNotReadModDll(fileName, reason); }
    public void CouldNotReadSptVersion(string reason) { _textRenderer.CouldNotReadSptVersion(reason); }
    public void PluginsDirectoryNotFound(string path) { _textRenderer.PluginsDirectoryNotFound(path); }

    public Task RunForgeQueryProgressAsync(int total, Func<Action<int>, Task> work, CancellationToken cancellationToken = default)
    {
        return _progressRenderer.RunForgeQueryProgressAsync(total, work, cancellationToken);
    }

    public Task<T> RunForgeQueryProgressAsync<T>(int total, Func<Action<int>, Task<T>> work, CancellationToken cancellationToken = default)
    {
        return _progressRenderer.RunForgeQueryProgressAsync(total, work, cancellationToken);
    }

    public void UsingPath(string path) { _textRenderer.UsingPath(path); }
    public void DirectoryDoesNotExist(string path) { _textRenderer.DirectoryDoesNotExist(path); }
    public void ValidatingSptVersion(string version) { _textRenderer.ValidatingSptVersion(version); }
    public void SptVersionValidated(string version) { _textRenderer.SptVersionValidated(version); }
    public void SptUpdateAvailable(SptVersionResult latest) { _textRenderer.SptUpdateAvailable(latest); }
    public void CheckModsExtendedUpdate(CheckModsExtendedUpdateResult result, SemanticVersioning.Version sptVersion) { _textRenderer.CheckModsExtendedUpdate(result, sptVersion); }
    public void NoModsFound() { _textRenderer.NoModsFound(); }

    public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion) { _versionTableRenderer.VersionCompatibilityResults(mods, sptVersion); }
    public void LoadingWarnings(List<Mod> modsWithWarnings) { _reconciliationRenderer.LoadingWarnings(modsWithWarnings); }
    public void ReconciliationResults(ModReconciliationResult result) { _reconciliationRenderer.ReconciliationResults(result); }
    public void MisplacedMods(MisplacedModReport report) { _misplacedModRenderer.MisplacedMods(report); }
    public void UnverifiedMods(List<Mod> mods) { _reconciliationRenderer.UnverifiedMods(mods); }
    public void DependencyResults(DependencyAnalysisResult result) { _dependencyRenderer.DependencyResults(result); }
    public void VersionTable(List<Mod> mods) { _versionTableRenderer.VersionTable(mods); }

    public bool PromptFetchRemoteIgnores() { return _promptService.PromptFetchRemoteIgnores(); }
    public void RemoteIgnoresMerged(int added) { _textRenderer.RemoteIgnoresMerged(added); }
    public void RemoteIgnoresUnavailable() { _textRenderer.RemoteIgnoresUnavailable(); }
    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates) { return _promptService.PromptEndOfRun(openableUpdateCount, canManageIgnoredUpdates); }
    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds) { return _promptService.SelectUpdatesToIgnore(candidates, preIgnoredApiModIds); }
    public void UpdatePagesOpened(int opened, int total) { _textRenderer.UpdatePagesOpened(opened, total); }
    public bool PromptReportIgnores() { return _promptService.PromptReportIgnores(); }
    public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled) { _textRenderer.IgnoreReportOpened(url, browserOpened, prefilled); }
    public void ApplicationFooter(string version, string hash, string logFilePath) { _textRenderer.ApplicationFooter(version, hash, logFilePath); }
    public void PendingConfirmationsSummary(IReadOnlyList<PendingConfirmation> pendingConfirmations) { _miscTableRenderer.PendingConfirmationsSummary(pendingConfirmations); }
    public Task<bool> PromptForConfirmationAsync(PendingConfirmation confirmation) { return _promptService.PromptForConfirmationAsync(confirmation); }
    public void IgnoreAddAlreadyIgnored(int apiModId, string localVersion, string latestVersion) { _textRenderer.IgnoreAddAlreadyIgnored(apiModId, localVersion, latestVersion); }
    public void IgnoreAddSuccess(int apiModId, string localVersion, string latestVersion) { _textRenderer.IgnoreAddSuccess(apiModId, localVersion, latestVersion); }
    public void IgnoreRemoveNotFound(int apiModId) { _textRenderer.IgnoreRemoveNotFound(apiModId); }
    public void IgnoreRemoveSuccess(int removedCount, int apiModId) { _textRenderer.IgnoreRemoveSuccess(removedCount, apiModId); }
    public void IgnoredUpdatesList(IReadOnlyList<IgnoredUpdate> ignores) { _miscTableRenderer.IgnoredUpdatesList(ignores); }
    public void InstalledModsList(IReadOnlyList<Mod> serverMods, IReadOnlyList<Mod> clientMods) { _miscTableRenderer.InstalledModsList(serverMods, clientMods); }
}
