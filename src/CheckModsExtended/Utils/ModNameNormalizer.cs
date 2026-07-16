using System.Collections.Immutable;
using Raffinert.FuzzySharp;

namespace CheckModsExtended.Utils;

/// <summary>
/// Provides centralized name normalization for mod matching operations.
/// </summary>
public static class ModNameNormalizer
{
    private static readonly char[] _charsToRemove = ['-', '_', ' ', '.'];

    /// <summary>
    /// Common suffixes used in mod names that are targeted for removal during normalization to improve match accuracy.
    /// Components like "server" or "plugin" are often omitted from API names but present locally.
    /// </summary>
    public static readonly ImmutableArray<string> SuffixesToRemove = ["server", "client", "plugin", "api", "backend", "frontend"];

    /// <summary>
    /// Normalizes a mod name for comparison by removing special characters,
    /// converting to lowercase, and optionally removing component suffixes (like server, client, backend, frontend).
    /// </summary>
    /// <param name="name">The name to normalize.</param>
    /// <param name="removeComponentSuffixes">Whether to remove component suffixes.</param>
    /// <returns>The normalized name string, or an empty string if the input was null or whitespace.</returns>
    public static string Normalize(string? name, bool removeComponentSuffixes = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var result = _charsToRemove.Aggregate(
            name.ToLowerInvariant(),
            (current, c) => current.Replace(c.ToString(), string.Empty)
        );

        if (removeComponentSuffixes)
        {
            var matchingSuffix = SuffixesToRemove.FirstOrDefault(s => result.EndsWith(s, StringComparison.Ordinal));
            if (matchingSuffix is not null)
            {
                result = result[..^matchingSuffix.Length];
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts a readable name from a mod GUID (e.g., "com.author.modname" -> "modname").
    /// </summary>
    /// <param name="guid">The GUID to extract from.</param>
    /// <returns>The extracted name, or the original GUID if extraction fails or is null/whitespace.</returns>
    public static string ExtractNameFromGuid(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            return string.Empty;
        }

        var parts = guid.Split(['.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return guid;
        }

        return parts[^1];
    }

    /// <summary>
    /// Calculates the fuzzy match score between two names.
    /// </summary>
    /// <param name="name1">The first name.</param>
    /// <param name="name2">The second name.</param>
    /// <returns>A score from 0-100 indicating similarity, or 0 if either name is invalid.</returns>
    public static int GetFuzzyMatchScore(string? name1, string? name2)
    {
        var normalized1 = Normalize(name1);
        var normalized2 = Normalize(name2);

        if (string.IsNullOrEmpty(normalized1) || string.IsNullOrEmpty(normalized2))
        {
            return 0;
        }

        return Fuzz.Ratio(normalized1, normalized2);
    }

    /// <summary>
    /// Determines if two names match exactly after normalization.
    /// </summary>
    /// <param name="name1">The first name.</param>
    /// <param name="name2">The second name.</param>
    /// <param name="removeComponentSuffixes">Whether to remove server/client suffixes.</param>
    /// <returns><c>true</c> if the names match exactly after normalization; otherwise, <c>false</c>.</returns>
    public static bool IsExactMatch(string? name1, string? name2, bool removeComponentSuffixes = false)
    {
        var normalized1 = Normalize(name1, removeComponentSuffixes);
        var normalized2 = Normalize(name2, removeComponentSuffixes);

        return !string.IsNullOrEmpty(normalized1) && string.Equals(normalized1, normalized2, StringComparison.Ordinal);
    }
}
