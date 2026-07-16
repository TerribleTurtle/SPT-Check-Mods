using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Reads and writes the local ignored-updates list and answers whether a given mod's update is currently ignored.
/// </summary>
public interface IIgnoredUpdateStore
{
    /// <summary>Loads the current entries (cached after first read). Returns empty on a missing or unreadable file.</summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The current list of ignored updates.</returns>
    Task<IReadOnlyList<IgnoredUpdate>> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Whether this mod's available update has been dismissed (matched on API id + both versions).</summary>
    /// <param name="mod">The mod to check for an ignored update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the mod's available update is ignored, otherwise false.</returns>
    Task<bool> IsIgnoredAsync(Mod mod, CancellationToken cancellationToken = default);

    /// <summary>Retrieves the ignored update entry if this mod's available update has been dismissed.</summary>
    /// <param name="mod">The mod to check for an ignored update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The ignored update entry if found, otherwise null.</returns>
    Task<IgnoredUpdate?> GetIgnoredUpdateAsync(Mod mod, CancellationToken cancellationToken = default);

    /// <summary>Replaces the stored entries with <paramref name="entries"/>.</summary>
    /// <param name="entries">The entries to replace the stored ones with.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveAsync(IReadOnlyList<IgnoredUpdate> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges entries from <paramref name="incoming"/> that are not already present, tagging added entries as <see cref="IgnoreSource.Remote"/>; returns the count added.
    /// </summary>
    /// <param name="incoming">The incoming entries to merge.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of new entries added.</returns>
    Task<int> MergeWithoutOverwriteAsync(
        IReadOnlyList<IgnoredUpdate> incoming,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Synchronizes the remote ignore list. Discards old remote entries, keeps user entries, and adds new remote entries.
    /// </summary>
    /// <param name="remoteList">The latest fetched remote list.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of remote entries applied.</returns>
    Task<int> SyncRemoteIgnoresAsync(
        IReadOnlyList<IgnoredUpdate> remoteList,
        CancellationToken cancellationToken = default
    );
}
