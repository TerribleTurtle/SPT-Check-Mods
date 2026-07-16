using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class RemoveLegacyApiKeyStepTests
{
    [Fact]
    public async Task ExecuteAsync_CallsRemoveOnRealService()
    {
        var logger = new FakeLogger<CheckModsExtended.Services.InitializationService>();
        var appPaths = new Microsoft.Extensions.Options.OptionsWrapper<CheckModsExtended.Configuration.AppPaths>(new CheckModsExtended.Configuration.AppPaths());
        var fakeFs = new DummyFileSystem();
        
        var service = new CheckModsExtended.Services.InitializationService(new FakeModCheckReporter(), logger, appPaths, fakeFs);
        var step = new RemoveLegacyApiKeyStep(service);

        var context = new UpdateWorkflowContext { Args = [] };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.True(fakeFs.FileExistsCalled);
    }
    
    private sealed class DummyFileSystem : CheckModsExtended.Utils.IFileSystem
    {
        public bool FileExistsCalled { get; set; }
        public bool FileExists(string path) { FileExistsCalled = true; return false; }
        public bool DirectoryExists(string path) => false;
        public string GetCurrentDirectory() => "";
        public string ReadAllText(string path) => "";
        public void WriteAllText(string path, string contents) {}
        public void DeleteFile(string path) {}
        public void CreateDirectory(string path) {}
        public string[] GetFiles(string path, string searchPattern, System.IO.SearchOption searchOption) => [];
        public string[] GetDirectories(string path) => [];
        public System.IO.Stream OpenRead(string path) => throw new System.NotImplementedException();
        public void CopyFile(string sourceFileName, string destFileName, bool overwrite) {}
        
        public System.IO.Stream Open(string path, System.IO.FileMode mode, System.IO.FileAccess access, System.IO.FileShare share) => throw new System.NotImplementedException();
        public Task DeleteFileAsync(string path, CancellationToken token = default) => Task.CompletedTask;
        public Task<string> ReadAllTextAsync(string path, CancellationToken token = default) => Task.FromResult("");
        public Task WriteAllTextAsync(string path, string contents, CancellationToken token = default) => Task.CompletedTask;
        public void MoveFile(string source, string dest, bool overwrite) {}
        public string? GetFileVersion(string path) => null;
        public long GetFileLength(string path) => 0;
    }
}
