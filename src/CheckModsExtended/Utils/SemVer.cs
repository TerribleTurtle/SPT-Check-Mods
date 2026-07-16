using CheckModsExtended.Models;
using OneOf;
using SemanticVersioning;

namespace CheckModsExtended.Utils;

/// <summary>
/// Helpers for parsing semantic version strings.
/// </summary>
public static class SemVer
{
    /// <summary>
    /// Parses a semantic version.
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <param name="context">Context about what this version represents, used for error reporting.</param>
    /// <returns>A <see cref="OneOf{T0, T1}"/> containing either the parsed <see cref="SemanticVersioning.Version"/> or an <see cref="InvalidSemVer"/> error.</returns>
    public static OneOf<SemanticVersioning.Version, InvalidSemVer> TryParse(string? version, string context)
    {
        if (string.IsNullOrWhiteSpace(version) || !SemanticVersioning.Version.TryParse(version, out var parsed))
        {
            return new InvalidSemVer(version, context);
        }

        return parsed;
    }

    /// <summary>
    /// Determines whether <paramref name="version"/> satisfies the given SPT version constraint (a semver range).
    /// Returns false when the constraint is missing or cannot be parsed.
    /// </summary>
    /// <param name="constraint">The semver range constraint (e.g. "~4.0.0").</param>
    /// <param name="version">The version to test against the constraint.</param>
    /// <returns>True if the version satisfies the constraint; false if it does not or the constraint is invalid.</returns>
    public static bool SatisfiesRange(string? constraint, SemanticVersioning.Version version)
    {
        return !string.IsNullOrWhiteSpace(constraint)
            && SemanticVersioning.Range.TryParse(constraint, out var range)
            && range.IsSatisfied(version);
    }

    /// <summary>
    /// Parses a semantic version from a string, returning 0.0.0 if the string is invalid or null.
    /// </summary>
    /// <param name="version">The version string to parse.</param>
    /// <returns>The parsed version or 0.0.0.</returns>
    public static SemanticVersioning.Version ParseOrDefault(this string? version)
    {
        if (string.IsNullOrWhiteSpace(version) || !SemanticVersioning.Version.TryParse(version, out var parsed))
        {
            return new SemanticVersioning.Version(0, 0, 0);
        }

        return parsed;
    }

    /// <summary>
    /// Determines whether two version strings represent the same logical version,
    /// handling differences like "1.0.0" vs "1.0.0.0" or "v1.0.0" vs "1.0.0".
    /// </summary>
    /// <param name="a">The first version string.</param>
    /// <param name="b">The second version string.</param>
    /// <returns>True if the versions are equivalent, otherwise false.</returns>
    public static bool AreVersionsEquivalent(string? a, string? b)
    {
        if (string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return false;
        }

        var aClean = a!.TrimStart('v', 'V');
        var bClean = b!.TrimStart('v', 'V');

        // Try System.Version first (handles 1.0.0 vs 1.0.0.0)
        if (System.Version.TryParse(aClean, out var va) && System.Version.TryParse(bClean, out var vb))
        {
            return NormalizeSystemVersion(va) == NormalizeSystemVersion(vb);
        }

        // Try SemanticVersioning if System.Version fails (handles 1.0.0-beta)
        if (SemanticVersioning.Version.TryParse(aClean, out var sva) && SemanticVersioning.Version.TryParse(bClean, out var svb))
        {
            return sva == svb;
        }

        return false;
    }

    private static System.Version NormalizeSystemVersion(System.Version v)
    {
        var build = v.Build >= 0 ? v.Build : 0;
        var revision = v.Revision >= 0 ? v.Revision : 0;
        return new System.Version(v.Major, v.Minor, build, revision);
    }
}
