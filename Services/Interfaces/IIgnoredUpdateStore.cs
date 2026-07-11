using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Reads and writes the local ignored-updates list and answers whether a given mod's update is currently ignored.
/// </summary>
public interface IIgnoredUpdateStore
{
    /// <summary>Loads the current entries (cached after first read). Returns empty on a missing or unreadable file.</summary>
    Task<IReadOnlyList<IgnoredUpdate>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether this mod's available update has been dismissed (matched on API id + both versions).</summary>
    Task<bool> IsIgnoredAsync(Mod mod, CancellationToken cancellationToken = default);

    /// <summary>Replaces the stored entries with <paramref name="entries"/>.</summary>
    Task SaveAsync(IReadOnlyList<IgnoredUpdate> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges entries from <paramref name="incoming"/> that are not already present, tagging added entries as <see cref="IgnoreSource.Remote"/>; returns the count added.
    /// </summary>
    Task<int> MergeWithoutOverwriteAsync(IReadOnlyList<IgnoredUpdate> incoming, CancellationToken cancellationToken = default);
}

