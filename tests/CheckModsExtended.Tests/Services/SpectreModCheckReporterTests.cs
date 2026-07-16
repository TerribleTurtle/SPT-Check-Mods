using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.UI;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public class SpectreModCheckReporterTests
{
    private sealed class DummyTextRenderer : ITextRenderer
    {
        public void Banner() {}
        public void Rule() {}
        public void Blank() {}
        public void Heading(string text) {}
        public void Status(string text) {}
        public void Success(string text) {}
        public void Warning(string text) {}
        public void Error(string text) {}
        public void Exception(Exception ex) {}
        public void ApiError(CheckModsExtended.Models.ApiError error) {}
        public void CouldNotReadModDll(string fileName, string reason) {}
        public void CouldNotReadSptVersion(string reason) {}
        public void PluginsDirectoryNotFound(string path) {}
        public void UsingPath(string path) {}
        public void DirectoryDoesNotExist(string path) {}
        public void ValidatingSptVersion(string version) {}
        public void SptVersionValidated(string version) {}
        public void SptUpdateAvailable(CheckModsExtended.Models.SptVersionResult latest) {}
        public void CheckModsExtendedUpdate(CheckModsExtended.Models.CheckModsExtendedUpdateResult result, SemanticVersioning.Version sptVersion) {}
        public void NoModsFound() {}
        public void RemoteIgnoresMerged(int added) {}
        public void RemoteIgnoresUnavailable() {}
        public void UpdatePagesOpened(int opened, int total) {}
        public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled) {}
        public void ApplicationFooter(string version, string hash, string logFilePath) {}
        public void IgnoreAddAlreadyIgnored(int apiModId, string localVersion, string latestVersion) {}
        public void IgnoreAddSuccess(int apiModId, string localVersion, string latestVersion) {}
        public void IgnoreRemoveNotFound(int apiModId) {}
        public void IgnoreRemoveSuccess(int removedCount, int apiModId) {}
    }

    private sealed class DummyProgressRenderer : IProgressRenderer
    {
        public Task RunForgeQueryProgressAsync(int total, Func<Action<int>, Task> work, CancellationToken cancellationToken = default) => work(_ => {});
        public Task<T> RunForgeQueryProgressAsync<T>(int total, Func<Action<int>, Task<T>> work, CancellationToken cancellationToken = default) => work(_ => {});
    }

    [Fact]
    public void Heading_CallsWebProgressTracker()
    {
        var tracker = new FakeWebProgressTracker();
        var config = new RuntimeConfig { Format = "table" };
        var reporter = new SpectreModCheckReporter(
            new DummyTextRenderer(),
            null!, null!, null!, null!, null!, null!, 
            new DummyProgressRenderer(),
            tracker,
            config
        );

        reporter.Heading("Test Heading");

        Assert.Equal(1, tracker.ReportStatusCallCount);
        Assert.Equal("Test Heading", tracker.LastReportedStatus);
    }
}

