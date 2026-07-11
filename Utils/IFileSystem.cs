using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Utils;

/// <summary>
/// Abstraction for file system operations.
/// </summary>
public interface IFileSystem
{
    bool FileExists(string path);
    Stream OpenRead(string path);
    Stream Open(string path, FileMode mode, FileAccess access, FileShare share);
    void DeleteFile(string path);
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);
    void MoveFile(string sourceFileName, string destFileName, bool overwrite);
    void CreateDirectory(string path);
    bool DirectoryExists(string path);
    string[] GetDirectories(string path);
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
    string GetCurrentDirectory();
    string? GetFileVersion(string path);
    long GetFileLength(string path);
}

/// <summary>
/// Default implementation of <see cref="IFileSystem"/>.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class FileSystem : IFileSystem
{
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }

    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
    {
        return new FileStream(path, mode, access, share);
    }

    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => File.Delete(path), cancellationToken);
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        return File.ReadAllTextAsync(path, cancellationToken);
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        return File.WriteAllTextAsync(path, contents, cancellationToken);
    }

    public void MoveFile(string sourceFileName, string destFileName, bool overwrite)
    {
        File.Move(sourceFileName, destFileName, overwrite);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public string[] GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    public string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    public string? GetFileVersion(string path)
    {
        return System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
    }

    public long GetFileLength(string path)
    {
        return new FileInfo(path).Length;
    }
}



