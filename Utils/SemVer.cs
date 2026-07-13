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
}
