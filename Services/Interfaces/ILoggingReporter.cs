using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles non-interactive logging and status reporting for the mod-check workflow.
/// </summary>
public interface ILoggingReporter
{
    void Banner();
    void Rule();
    void Blank();
    void Heading(string text);
    void Status(string text);
    void Success(string text);
    void Warning(string text);
    void Error(string text);
    void CouldNotReadModDll(string fileName, string reason);
    void CouldNotReadSptVersion(string reason);
    void PluginsDirectoryNotFound(string path);
    Task RunForgeQueryProgressAsync(int total, Func<Action<int>, Task> work, CancellationToken cancellationToken = default);
    Task<T> RunForgeQueryProgressAsync<T>(int total, Func<Action<int>, Task<T>> work, CancellationToken cancellationToken = default);
    void UsingPath(string path);
    void DirectoryDoesNotExist(string path);
    void ValidatingSptVersion(string version);
    void SptVersionValidated(string version);
    void SptUpdateAvailable(SptVersionResult latest);
    void CheckModsExtendedUpdate(CheckModsExtendedUpdateResult result, SemanticVersioning.Version sptVersion);
    void NoModsFound();
    void Exception(Exception ex);
    void RemoteIgnoresMerged(int added);
    void RemoteIgnoresUnavailable();
    void UpdatePagesOpened(int opened, int total);
    void IgnoreReportOpened(string url, bool browserOpened, bool prefilled);
    void ApplicationFooter(string version, string hash, string logFilePath);
    void IgnoreAddAlreadyIgnored(int apiModId, string localVersion, string latestVersion);
    void IgnoreAddSuccess(int apiModId, string localVersion, string latestVersion);
    void IgnoreRemoveNotFound(int apiModId);
    void IgnoreRemoveSuccess(int removedCount, int apiModId);
}
