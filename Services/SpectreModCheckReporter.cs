using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.UI;
using CheckModsExtended.Services.Web;
using SemanticVersioning;

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
    private readonly IWebProgressTracker _webProgressTracker;
    private readonly RuntimeConfig _runtimeConfig;

    public SpectreModCheckReporter(
        ITextRenderer textRenderer,
        IVersionTableUiRenderer versionTableRenderer,
        IReconciliationUiRenderer reconciliationRenderer,
        IMisplacedModUiRenderer misplacedModRenderer,
        IDependencyUiRenderer dependencyRenderer,
        IMiscTableUiRenderer miscTableRenderer,
        IUserPromptService promptService,
        IProgressRenderer progressRenderer,
        IWebProgressTracker webProgressTracker,
        RuntimeConfig runtimeConfig
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
        _webProgressTracker = webProgressTracker;
        _runtimeConfig = runtimeConfig;
    }

    private bool ShouldRender()
    {
        return _runtimeConfig.Format.Equals("table", StringComparison.OrdinalIgnoreCase);
    }

    public void Banner()
    {
        if (ShouldRender())
        {
            _textRenderer.Banner();
        }
    }

    public void Rule()
    {
        if (ShouldRender())
        {
            _textRenderer.Rule();
        }
    }

    public void Blank()
    {
        if (ShouldRender())
        {
            _textRenderer.Blank();
        }
    }

    public void Heading(string text)
    {
        _webProgressTracker?.ReportStatus(text);
        if (ShouldRender())
        {
            _textRenderer.Heading(text);
        }
    }

    public void Status(string text)
    {
        if (ShouldRender())
        {
            _textRenderer.Status(text);
        }
    }

    public void Success(string text)
    {
        if (ShouldRender())
        {
            _textRenderer.Success(text);
        }
    }

    public void Warning(string text)
    {
        if (ShouldRender())
        {
            _textRenderer.Warning(text);
        }
    }

        public void ApiError(ApiError error)
    {
        if (ShouldRender())
        {
            _textRenderer.ApiError(error);
        }
    }

    public void Error(string text)
    {
        if (ShouldRender())
        {
            _textRenderer.Error(text);
        }
    }

    public void Exception(Exception ex)
    {
        if (ShouldRender())
        {
            _textRenderer.Exception(ex);
        }
    }

    public void CouldNotReadModDll(string fileName, string reason)
    {
        if (ShouldRender())
        {
            _textRenderer.CouldNotReadModDll(fileName, reason);
        }
    }

    public void CouldNotReadSptVersion(string reason)
    {
        if (ShouldRender())
        {
            _textRenderer.CouldNotReadSptVersion(reason);
        }
    }

    public void PluginsDirectoryNotFound(string path)
    {
        if (ShouldRender())
        {
            _textRenderer.PluginsDirectoryNotFound(path);
        }
    }

    public Task RunForgeQueryProgressAsync(
        int total,
        Func<Action<int>, Task> work,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldRender())
        {
            return work(_ => { });
        }
        return _progressRenderer.RunForgeQueryProgressAsync(total, work, cancellationToken);
    }

    public Task<T> RunForgeQueryProgressAsync<T>(
        int total,
        Func<Action<int>, Task<T>> work,
        CancellationToken cancellationToken = default
    )
    {
        if (!ShouldRender())
        {
            return work(_ => { });
        }
        return _progressRenderer.RunForgeQueryProgressAsync(total, work, cancellationToken);
    }

    public void UsingPath(string path)
    {
        if (ShouldRender())
        {
            _textRenderer.UsingPath(path);
        }
    }

    public void DirectoryDoesNotExist(string path)
    {
        if (ShouldRender())
        {
            _textRenderer.DirectoryDoesNotExist(path);
        }
    }

    public void ValidatingSptVersion(string version)
    {
        if (ShouldRender())
        {
            _textRenderer.ValidatingSptVersion(version);
        }
    }

    public void SptVersionValidated(string version)
    {
        if (ShouldRender())
        {
            _textRenderer.SptVersionValidated(version);
        }
    }

    public void SptUpdateAvailable(SptVersionResult latest)
    {
        if (ShouldRender())
        {
            _textRenderer.SptUpdateAvailable(latest);
        }
    }

    public void CheckModsExtendedUpdate(CheckModsExtendedUpdateResult result, SemanticVersioning.Version sptVersion)
    {
        if (ShouldRender())
        {
            _textRenderer.CheckModsExtendedUpdate(result, sptVersion);
        }
    }

    public void NoModsFound()
    {
        if (ShouldRender())
        {
            _textRenderer.NoModsFound();
        }
    }

    public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion)
    {
        if (ShouldRender())
        {
            _versionTableRenderer.VersionCompatibilityResults(mods, sptVersion);
        }
    }

    public void LoadingWarnings(List<Mod> modsWithWarnings)
    {
        if (ShouldRender())
        {
            _reconciliationRenderer.LoadingWarnings(modsWithWarnings);
        }
    }

    public void ReconciliationResults(ModReconciliationResult result)
    {
        if (ShouldRender())
        {
            _reconciliationRenderer.ReconciliationResults(result);
        }
    }

    public void MisplacedMods(MisplacedModReport report)
    {
        if (ShouldRender())
        {
            _misplacedModRenderer.MisplacedMods(report);
        }
    }

    public void UnverifiedMods(List<Mod> mods)
    {
        if (ShouldRender())
        {
            _reconciliationRenderer.UnverifiedMods(mods);
        }
    }

    public void DependencyResults(DependencyAnalysisResult result)
    {
        if (ShouldRender())
        {
            _dependencyRenderer.DependencyResults(result);
        }
    }

    public void VersionTable(List<Mod> mods)
    {
        if (ShouldRender())
        {
            _versionTableRenderer.VersionTable(mods);
        }
    }

    public void CachedVersionTable(IReadOnlyList<ModDto> mods)
    {
        if (ShouldRender())
        {
            _versionTableRenderer.CachedVersionTable(mods);
        }
    }

    public bool PromptFetchRemoteIgnores()
    {
        return _promptService.PromptFetchRemoteIgnores();
    }

    public void RemoteIgnoresMerged(int added)
    {
        if (ShouldRender())
        {
            _textRenderer.RemoteIgnoresMerged(added);
        }
    }

    public void RemoteIgnoresUnavailable()
    {
        if (ShouldRender())
        {
            _textRenderer.RemoteIgnoresUnavailable();
        }
    }

    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates)
    {
        return _promptService.PromptEndOfRun(openableUpdateCount, canManageIgnoredUpdates);
    }

    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds)
    {
        return _promptService.SelectUpdatesToIgnore(candidates, preIgnoredApiModIds);
    }

    public void UpdatePagesOpened(int opened, int total)
    {
        if (ShouldRender())
        {
            _textRenderer.UpdatePagesOpened(opened, total);
        }
    }

    public bool PromptReportIgnores()
    {
        return _promptService.PromptReportIgnores();
    }

    public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled)
    {
        if (ShouldRender())
        {
            _textRenderer.IgnoreReportOpened(url, browserOpened, prefilled);
        }
    }

    public void ApplicationFooter(string version, string hash, string logFilePath)
    {
        if (ShouldRender())
        {
            _textRenderer.ApplicationFooter(version, hash, logFilePath);
        }
    }

    public void PendingConfirmationsSummary(IReadOnlyList<PendingConfirmation> pendingConfirmations)
    {
        if (ShouldRender())
        {
            _miscTableRenderer.PendingConfirmationsSummary(pendingConfirmations);
        }
    }

    public Task<bool> PromptForConfirmationAsync(PendingConfirmation confirmation)
    {
        return _promptService.PromptForConfirmationAsync(confirmation);
    }

    public bool PromptLoadFromCache(DateTimeOffset cacheTime)
    {
        return _promptService.PromptLoadFromCache(cacheTime);
    }

    public void IgnoreAddAlreadyIgnored(int apiModId, string localVersion, string latestVersion)
    {
        if (ShouldRender())
        {
            _textRenderer.IgnoreAddAlreadyIgnored(apiModId, localVersion, latestVersion);
        }
    }

    public void IgnoreAddSuccess(int apiModId, string localVersion, string latestVersion)
    {
        if (ShouldRender())
        {
            _textRenderer.IgnoreAddSuccess(apiModId, localVersion, latestVersion);
        }
    }

    public void IgnoreRemoveNotFound(int apiModId)
    {
        if (ShouldRender())
        {
            _textRenderer.IgnoreRemoveNotFound(apiModId);
        }
    }

    public void IgnoreRemoveSuccess(int removedCount, int apiModId)
    {
        if (ShouldRender())
        {
            _textRenderer.IgnoreRemoveSuccess(removedCount, apiModId);
        }
    }

    public void IgnoredUpdatesList(IReadOnlyList<IgnoredUpdate> ignores, ListFilterOptions? options = null)
    {
        if (ShouldRender())
        {
            _miscTableRenderer.IgnoredUpdatesList(ignores, options);
        }
    }

    public void InstalledModsList(
        IReadOnlyList<Mod> serverMods,
        IReadOnlyList<Mod> clientMods,
        ListFilterOptions? options = null
    )
    {
        if (ShouldRender())
        {
            _miscTableRenderer.InstalledModsList(serverMods, clientMods, options);
        }
    }
}



