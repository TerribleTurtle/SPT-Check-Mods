using System;
using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.UI;

namespace CheckMods.Tests.Fakes;

public sealed class FakeTextRenderer : ITextRenderer
{
    public bool RuleCalled { get; private set; }

    public void Banner() { }

    public void Rule() => RuleCalled = true;

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

    public void CheckModsUpdate(CheckModsUpdateResult result, SemanticVersioning.Version sptVersion) { }

    public void NoModsFound() { }

    public bool PromptFetchRemoteIgnores() => false;

    public void RemoteIgnoresMerged(int added) { }

    public void RemoteIgnoresUnavailable() { }

    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates) => EndOfRunChoice.Exit;

    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds) =>
        new List<Mod>();

    public void UpdatePagesOpened(int opened, int total) { }

    public bool PromptReportIgnores() => false;

    public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled) { }
}
