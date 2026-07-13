using CheckModsExtended.Configuration;
using Spectre.Console;

namespace CheckModsExtended.Services.UI;

/// <summary>
/// Shared formatting utility for rendering UI components.
/// </summary>
public static class UiFormattingUtility
{
    /// <summary>
    /// Formats mod name and author for display, truncating them to max lengths.
    /// </summary>
    public static (string displayName, string displayAuthor) FormatModDisplayStrings(string modName, string author)
    {
        var displayName =
            modName.Length > MatchingConstants.MaxDisplayNameLength
                ? modName[..(MatchingConstants.MaxDisplayNameLength - 3)] + "..."
                : modName;
        var displayAuthor =
            author.Length > MatchingConstants.MaxDisplayAuthorLength
                ? author[..(MatchingConstants.MaxDisplayAuthorLength - 3)] + "..."
                : author;

        return (displayName, displayAuthor);
    }

    /// <summary>
    /// Formats a mod name as a clickable link if the URL is safe, otherwise escapes it.
    /// </summary>
    public static string FormatModLink(string name, string? apiUrl)
    {
        var escaped = name.EscapeMarkup();
        if (IsLinkUrlSafe(apiUrl))
        {
            return $"[white link={apiUrl}]{escaped}[/]";
        }
        return $"[white]{escaped}[/]";
    }

    /// <summary>
    /// Wraps markup in a clickable link if the URL is safe.
    /// </summary>
    public static string WithLink(string displayMarkup, string? url)
    {
        return IsLinkUrlSafe(url) ? $"[link={url}]{displayMarkup}[/]" : displayMarkup;
    }

    /// <summary>
    /// Checks if a URL is safe to use in a Spectre.Console link tag.
    /// </summary>
    public static bool IsLinkUrlSafe(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) && !url.Contains('[') && !url.Contains(']');
    }
}
