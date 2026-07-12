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
    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The file to check.</param>
    /// <returns>true if the caller has the required permissions and path contains the name of an existing file; otherwise, false.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Opens an existing file for reading.
    /// </summary>
    /// <param name="path">The file to be opened for reading.</param>
    /// <returns>A read-only System.IO.FileStream on the specified path.</returns>
    Stream OpenRead(string path);

    /// <summary>
    /// Opens a file with the specified mode, access, and share settings.
    /// </summary>
    /// <param name="path">The file to open.</param>
    /// <param name="mode">A FileMode value that specifies whether a file is created if one does not exist, and determines whether the contents of existing files are retained or overwritten.</param>
    /// <param name="access">A FileAccess value that specifies the operations that can be performed on the file.</param>
    /// <param name="share">A FileShare value specifying the type of access other threads have to the file.</param>
    /// <returns>A Stream on the specified path, having the specified mode with read, write, or read/write access and the specified sharing option.</returns>
    Stream Open(string path, FileMode mode, FileAccess access, FileShare share);

    /// <summary>
    /// Deletes the specified file.
    /// </summary>
    /// <param name="path">The name of the file to be deleted. Wildcard characters are not supported.</param>
    void DeleteFile(string path);

    /// <summary>
    /// Asynchronously deletes the specified file.
    /// </summary>
    /// <param name="path">The name of the file to be deleted.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteFileAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously opens a text file, reads all the text in the file into a string, and then closes the file.
    /// </summary>
    /// <param name="path">The file to open for reading.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation, which wraps the string containing all text in the file.</returns>
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously creates a new file, writes the specified string to the file, and then closes the file. If the target file already exists, it is overwritten.
    /// </summary>
    /// <param name="path">The file to write to.</param>
    /// <param name="contents">The string to write to the file.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a specified file to a new location, providing the option to specify a new file name.
    /// </summary>
    /// <param name="sourceFileName">The name of the file to move. Can include a relative or absolute path.</param>
    /// <param name="destFileName">The new path and name for the file.</param>
    /// <param name="overwrite">true to overwrite the destination file if it already exists; false otherwise.</param>
    void MoveFile(string sourceFileName, string destFileName, bool overwrite);

    /// <summary>
    /// Creates all directories and subdirectories in the specified path unless they already exist.
    /// </summary>
    /// <param name="path">The directory to create.</param>
    void CreateDirectory(string path);

    /// <summary>
    /// Determines whether the given path refers to an existing directory on disk.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns>true if path refers to an existing directory; otherwise, false.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Returns the names of subdirectories (including their paths) in the specified directory.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <returns>An array of the full names (including paths) of subdirectories in the specified path, or an empty array if no directories are found.</returns>
    string[] GetDirectories(string path);

    /// <summary>
    /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory, using a value to determine whether to search subdirectories.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in path.</param>
    /// <param name="searchOption">One of the enumeration values that specifies whether the search operation should include only the current directory or should include all subdirectories.</param>
    /// <returns>An array of the full names (including paths) for the files in the specified directory that match the specified search pattern and option.</returns>
    string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

    /// <summary>
    /// Gets the current working directory of the application.
    /// </summary>
    /// <returns>A string that contains the path of the current working directory.</returns>
    string GetCurrentDirectory();

    /// <summary>
    /// Retrieves the file version information for the specified file.
    /// </summary>
    /// <param name="path">The path of the file to retrieve version information for.</param>
    /// <returns>The file version, or null if the file does not contain version information.</returns>
    string? GetFileVersion(string path);

    /// <summary>
    /// Gets the size, in bytes, of the specified file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>The size of the specified file in bytes.</returns>
    long GetFileLength(string path);
}

/// <summary>
/// Default implementation of <see cref="IFileSystem"/>.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class FileSystem : IFileSystem
{
    /// <inheritdoc />
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    /// <inheritdoc />
    public Stream OpenRead(string path)
    {
        return File.OpenRead(path);
    }

    /// <inheritdoc />
    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
    {
        return new FileStream(path, mode, access, share);
    }

    /// <inheritdoc />
    public void DeleteFile(string path)
    {
        File.Delete(path);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => File.Delete(path), cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        return File.ReadAllTextAsync(path, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        return File.WriteAllTextAsync(path, contents, cancellationToken);
    }

    /// <inheritdoc />
    public void MoveFile(string sourceFileName, string destFileName, bool overwrite)
    {
        File.Move(sourceFileName, destFileName, overwrite);
    }

    /// <inheritdoc />
    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    /// <inheritdoc />
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    /// <inheritdoc />
    public string[] GetDirectories(string path)
    {
        return Directory.GetDirectories(path);
    }

    /// <inheritdoc />
    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        return Directory.GetFiles(path, searchPattern, searchOption);
    }

    /// <inheritdoc />
    public string GetCurrentDirectory()
    {
        return Directory.GetCurrentDirectory();
    }

    /// <inheritdoc />
    public string? GetFileVersion(string path)
    {
        return System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
    }

    /// <inheritdoc />
    public long GetFileLength(string path)
    {
        return new FileInfo(path).Length;
    }
}



