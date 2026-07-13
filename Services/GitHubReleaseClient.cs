using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services;

public sealed class GitHubReleaseClient(HttpClient httpClient, ILogger<GitHubReleaseClient> logger) : IGitHubReleaseClient
{
    internal sealed record GitHubReleaseResponse(
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets
    );

    internal sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("browser_download_url")] string? BrowserDownloadUrl
    );

    public async Task<string?> TryGetLatestReleaseAssetUrlAsync(string sourceCodeUrl, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(sourceCodeUrl))
        {
            return null;
        }

        if (!Uri.TryCreate(sourceCodeUrl, UriKind.Absolute, out var uri) ||
            !uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.Segments;
        if (segments.Length < 3)
        {
            return null;
        }

        var owner = segments[1].Trim('/');
        var repo = segments[2].Trim('/');

        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            repo = repo.Substring(0, repo.Length - 4);
        }

        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";

        try
        {
            logger.LogDebug("Querying GitHub API for latest release of {Owner}/{Repo}", owner, repo);

            var response = await httpClient.GetAsync(apiUrl, token);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogWarning("GitHub API rate limit hit when querying {Repo}. Falling back gracefully.", repo);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("GitHub API returned {StatusCode} for {Repo}", response.StatusCode, repo);
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync(
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.GitHubReleaseResponse,
                cancellationToken: token);
            if (release?.Assets == null)
            {
                return null;
            }

            // Find an asset that looks like an archive
            var asset = release.Assets.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(a.Name) &&
                (a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ||
                 a.Name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase) ||
                 a.Name.EndsWith(".rar", StringComparison.OrdinalIgnoreCase)));

            return asset?.BrowserDownloadUrl;
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException)
        {
            logger.LogDebug(ex, "Failed to fetch GitHub release asset for {Repo}", repo);
            return null;
        }
    }
}
