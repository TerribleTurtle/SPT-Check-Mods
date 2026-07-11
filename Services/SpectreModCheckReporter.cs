using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Services.UI;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services;

/// <summary>
/// Spectre.Console implementation of <see cref="IModCheckReporter"/>.
/// Acts as a facade delegating to specific UI renderers.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class SpectreModCheckReporter : IModCheckReporter
{
    private readonly ITextRenderer _textRenderer;
    private readonly ITableRenderer _tableRenderer;
    private readonly IProgressRenderer _progressRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreModCheckReporter"/> class.
    /// </summary>
    /// <param name="textRenderer">The text renderer.</param>
    /// <param name="tableRenderer">The table renderer.</param>
    /// <param name="progressRenderer">The progress renderer.</param>
    public SpectreModCheckReporter(
        ITextRenderer textRenderer,
        ITableRenderer tableRenderer,
        IProgressRenderer progressRenderer
    )
    {
        _textRenderer = textRenderer;
        _tableRenderer = tableRenderer;
        _progressRenderer = progressRenderer;
    }

    /// <inheritdoc />
    public void Banner()
    {
        _textRenderer.Banner();
    }

    /// <inheritdoc />
    public void Rule()
    {
        _textRenderer.Rule();
    }

    /// <inheritdoc />
    public void Blank()
    {
        _textRenderer.Blank();
    }

    /// <inheritdoc />
    public void Heading(string text)
    {
        _textRenderer.Heading(text);
    }

    /// <inheritdoc />
    public void Status(string text)
    {
        _textRenderer.Status(text);
    }

    /// <inheritdoc />
    public void Success(string text)
    {
        _textRenderer.Success(text);
    }

    /// <inheritdoc />
    public void Warning(string text)
    {
        _textRenderer.Warning(text);
    }

    /// <inheritdoc />
    public void Error(string text)
    {
        _textRenderer.Error(text);
    }

    /// <inheritdoc />
    public void Exception(Exception ex)
    {
        _textRenderer.Exception(ex);
    }

    /// <inheritdoc />
    public void CouldNotReadModDll(string fileName, string reason)
    {
        _textRenderer.CouldNotReadModDll(fileName, reason);
    }

    /// <inheritdoc />
    public void CouldNotReadSptVersion(string reason)
    {
        _textRenderer.CouldNotReadSptVersion(reason);
    }

    /// <inheritdoc />
    public void PluginsDirectoryNotFound(string path)
    {
        _textRenderer.PluginsDirectoryNotFound(path);
    }

    /// <inheritdoc />
    public Task RunForgeQueryProgressAsync(int total, Func<Action<int>, Task> work, CancellationToken cancellationToken = default)
    {
        return _progressRenderer.RunForgeQueryProgressAsync(total, work, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T> RunForgeQueryProgressAsync<T>(int total, Func<Action<int>, Task<T>> work, CancellationToken cancellationToken = default)
    {
        return _progressRenderer.RunForgeQueryProgressAsync(total, work, cancellationToken);
    }

    /// <inheritdoc />
    public void UsingPath(string path)
    {
        _textRenderer.UsingPath(path);
    }

    /// <inheritdoc />
    public void DirectoryDoesNotExist(string path)
    {
        _textRenderer.DirectoryDoesNotExist(path);
    }

    /// <inheritdoc />
    public void ValidatingSptVersion(string version)
    {
        _textRenderer.ValidatingSptVersion(version);
    }

    /// <inheritdoc />
    public void SptVersionValidated(string version)
    {
        _textRenderer.SptVersionValidated(version);
    }

    /// <inheritdoc />
    public void SptUpdateAvailable(SptVersionResult latest)
    {
        _textRenderer.SptUpdateAvailable(latest);
    }

    /// <inheritdoc />
    public void CheckModsUpdate(CheckModsUpdateResult result, SemanticVersioning.Version sptVersion)
    {
        _textRenderer.CheckModsUpdate(result, sptVersion);
    }

    /// <inheritdoc />
    public void NoModsFound()
    {
        _textRenderer.NoModsFound();
    }

    /// <inheritdoc />
    public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion)
    {
        _tableRenderer.VersionCompatibilityResults(mods, sptVersion);
    }

    /// <inheritdoc />
    public void LoadingWarnings(List<Mod> modsWithWarnings)
    {
        _tableRenderer.LoadingWarnings(modsWithWarnings);
    }

    /// <inheritdoc />
    public void ReconciliationResults(ModReconciliationResult result)
    {
        _tableRenderer.ReconciliationResults(result);
    }

    /// <inheritdoc />
    public void MisplacedMods(MisplacedModReport report)
    {
        _tableRenderer.MisplacedMods(report);
    }

    /// <inheritdoc />
    public void UnverifiedMods(List<Mod> mods)
    {
        _tableRenderer.UnverifiedMods(mods);
    }

    /// <inheritdoc />
    public void DependencyResults(DependencyAnalysisResult result)
    {
        _tableRenderer.DependencyResults(result);
    }

    /// <inheritdoc />
    public void VersionTable(List<Mod> mods)
    {
        _tableRenderer.VersionTable(mods);
    }

    /// <inheritdoc />
    public bool PromptFetchRemoteIgnores()
    {
        return _textRenderer.PromptFetchRemoteIgnores();
    }

    /// <inheritdoc />
    public void RemoteIgnoresMerged(int added)
    {
        _textRenderer.RemoteIgnoresMerged(added);
    }

    /// <inheritdoc />
    public void RemoteIgnoresUnavailable()
    {
        _textRenderer.RemoteIgnoresUnavailable();
    }

    /// <inheritdoc />
    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates)
    {
        return _textRenderer.PromptEndOfRun(openableUpdateCount, canManageIgnoredUpdates);
    }

    /// <inheritdoc />
    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds)
    {
        return _textRenderer.SelectUpdatesToIgnore(candidates, preIgnoredApiModIds);
    }

    /// <inheritdoc />
    public void UpdatePagesOpened(int opened, int total)
    {
        _textRenderer.UpdatePagesOpened(opened, total);
    }

    /// <inheritdoc />
    public bool PromptReportIgnores()
    {
        return _textRenderer.PromptReportIgnores();
    }

    /// <inheritdoc />
    public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled)
    {
        _textRenderer.IgnoreReportOpened(url, browserOpened, prefilled);
    }

    /// <inheritdoc />
    public void ApplicationFooter(string version, string hash, string logFilePath)
    {
        _textRenderer.ApplicationFooter(version, hash, logFilePath);
    }
}
