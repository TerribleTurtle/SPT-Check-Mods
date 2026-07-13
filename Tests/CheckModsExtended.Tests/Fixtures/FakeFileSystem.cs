using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Utils;

namespace CheckModsExtended.Tests.Fixtures;

public class FakeFileSystem : IFileSystem
{
    public readonly Dictionary<string, byte[]> Files = new(StringComparer.OrdinalIgnoreCase);
    public readonly HashSet<string> Directories = new(StringComparer.OrdinalIgnoreCase);

    public bool FileExists(string path) => Files.ContainsKey(path);

    public Stream OpenRead(string path)
    {
        if (!Files.TryGetValue(path, out var content))
            throw new FileNotFoundException("File not found.", path);
        return new MemoryStream(content);
    }

    public Stream Open(string path, FileMode mode, FileAccess access, FileShare share)
    {
        if (mode == FileMode.Create || mode == FileMode.OpenOrCreate)
        {
            var ms = new MemoryStream();
            return ms;
        }
        return OpenRead(path);
    }

    public void DeleteFile(string path)
    {
        Files.Remove(path);
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        Files.Remove(path);
        return Task.CompletedTask;
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!Files.TryGetValue(path, out var content))
            throw new FileNotFoundException("File not found.", path);
        return Task.FromResult(System.Text.Encoding.UTF8.GetString(content));
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
    {
        Files[path] = System.Text.Encoding.UTF8.GetBytes(contents);
        return Task.CompletedTask;
    }

    public void MoveFile(string sourceFileName, string destFileName, bool overwrite)
    {
        if (!Files.TryGetValue(sourceFileName, out var content))
            throw new FileNotFoundException("File not found.", sourceFileName);

        if (!overwrite && Files.ContainsKey(destFileName))
            throw new IOException("File already exists.");

        Files[destFileName] = content;
        Files.Remove(sourceFileName);
    }

    public void CreateDirectory(string path)
    {
        var current = path;
        while (!string.IsNullOrEmpty(current))
        {
            Directories.Add(current);
            current = Path.GetDirectoryName(current);
        }
    }

    public bool DirectoryExists(string path) => Directories.Contains(path);

    public string[] GetDirectories(string path)
    {
        var normalizedPath =
            path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Directories
            .Where(d =>
                !string.Equals(d, path, StringComparison.OrdinalIgnoreCase)
                && d.StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase)
            )
            .Where(d =>
                d.Substring(normalizedPath.Length)
                    .IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) == -1
            )
            .ToArray();
    }

    public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
    {
        var normalizedPath =
            path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Files
            .Keys.Where(f => f.StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
            .Where(f =>
                searchOption == SearchOption.AllDirectories
                || f.Substring(normalizedPath.Length)
                    .IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) == -1
            )
            .Where(f =>
            {
                if (searchPattern == "*")
                    return true;
                if (
                    searchPattern.StartsWith("*")
                    && f.EndsWith(searchPattern.Substring(1), StringComparison.OrdinalIgnoreCase)
                )
                    return true;
                return true; // Simplified pattern matching
            })
            .ToArray();
    }

    public string GetCurrentDirectory() => Path.GetFullPath("MockDir");

    public string GetFileVersion(string path) => "3.10.0";

    public long GetFileLength(string path) => Files.TryGetValue(path, out var c) ? c.Length : 0;
}
