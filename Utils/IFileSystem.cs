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
}
