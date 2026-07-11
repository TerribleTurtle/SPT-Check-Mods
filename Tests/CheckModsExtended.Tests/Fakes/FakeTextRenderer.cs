using System;
using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services.UI;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeTextRenderer : ITextRenderer
{
    public bool RuleCalled { get; private set; }

    public void Banner() { }

    public void Rule()
    {
        RuleCalled = true;
    }

    public void Blank() { }

    public void Heading(string text) { }

    public void Status(string text) { }

    public void Success(string text) { }

    public void Warning(string text) { }

    public void Error(string text) { }

    public void Exception(Exception ex) { }

    public void CouldNotReadModDll(string fileName, string reason) { }

    public void CouldNotReadSptVersion(string reason) { }

    public void PluginsDirectoryNotFound(string path) { }

    public void UsingPath(string path) { }

    public void DirectoryDoesNotExist(string path) { }

    public void ValidatingSptVersion(string version) { }

    public void SptVersionValidated(string version) { }

    public void SptUpdateAvailable(SptVersionResult latest) { }

    public void CheckModsExtendedUpdate(CheckModsExtendedUpdateResult result, SemanticVersioning.Version sptVersion) { }

    public void NoModsFound() { }

    public bool PromptFetchRemoteIgnores()
    {
        return false;
    }

    public void RemoteIgnoresMerged(int added) { }

    public void RemoteIgnoresUnavailable() { }

    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates)
    {
        return EndOfRunChoice.Exit;
    }

    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds)
    {
        return new List<Mod>();
    }

    public void UpdatePagesOpened(int opened, int total) { }

    public bool PromptReportIgnores()
    {
        return false;
    }

    public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled) { }

    public void ApplicationFooter(string version, string hash, string logFilePath) { }

    public void IgnoreAddAlreadyIgnored(int apiModId, string localVersion, string latestVersion) { }
    public void IgnoreAddSuccess(int apiModId, string localVersion, string latestVersion) { }
    public void IgnoreRemoveNotFound(int apiModId) { }
    public void IgnoreRemoveSuccess(int removedCount, int apiModId) { }
    public System.Threading.Tasks.Task<bool> PromptForConfirmationAsync(PendingConfirmation confirmation) => System.Threading.Tasks.Task.FromResult(true);
}
