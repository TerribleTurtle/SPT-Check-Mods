namespace CheckModsExtended.Utils;

/// <summary>
/// Path validation utilities that prevent directory traversal.
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// Validates and returns a safe absolute path, preventing directory traversal attacks. Resolves relative path
    /// segments and ensures the result stays within the base path if provided.
    /// </summary>
    /// <param name="inputPath">The input path to validate and sanitize.</param>
    /// <param name="basePath">Optional base path to restrict the result to. If provided, the result must be within this path.</param>
    /// <returns>A safe absolute path or null if the input is invalid or represents a directory traversal attempt.</returns>
    public static string? GetSafePath(string? inputPath, string? basePath = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return null;
        }

        try
        {
            // If no base path is provided, we just resolve and return the absolute path.
            // (Used by InitializationService when determining the raw SPT path)
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return Path.GetFullPath(inputPath);
            }

            var baseFullPath = Path.GetFullPath(basePath);
            
            // Resolve the input path relative to the base path, not the current working directory.
            var fullPath = Path.GetFullPath(inputPath, baseFullPath);

            var relativePath = Path.GetRelativePath(baseFullPath, fullPath);

            // Path.GetRelativePath returns "." if the paths are the same.
            // If the resolved path escapes the base directory, it will start with ".."
            // E.g., "..", "..\something", "../something"
            if (relativePath == ".." || 
                relativePath.StartsWith(".." + Path.DirectorySeparatorChar) || 
                relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar) ||
                Path.IsPathRooted(relativePath))
            {
                return null;
            }

            return fullPath;
        }
        catch (ArgumentException)
        {
            return null; // Invalid path characters
        }
        catch (PathTooLongException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null; // Path contains a colon in the middle of the string
        }
    }
}

