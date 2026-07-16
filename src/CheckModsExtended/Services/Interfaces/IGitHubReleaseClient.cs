using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Client for safely fetching release assets from the GitHub API.
/// </summary>
public interface IGitHubReleaseClient
{
    /// <summary>
    /// Attempts to fetch the direct download URL for a release asset (e.g., .zip) from the latest GitHub release.
    /// Returns null if the source URL is not GitHub, if the API rate limit is hit, or if no asset is found.
    /// </summary>
    /// <param name="sourceCodeUrl">The source code URL to parse the repository from.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The direct asset download URL, or null.</returns>
    Task<string?> TryGetLatestReleaseAssetUrlAsync(string sourceCodeUrl, CancellationToken token = default);

    /// <summary>
    /// Attempts to fetch the latest release version and its URL from a GitHub repository.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The latest version tag and HTML URL, or null if it fails.</returns>
    Task<(string? Version, string? Url)> GetLatestReleaseVersionAsync(string owner, string repo, CancellationToken token = default);
}
