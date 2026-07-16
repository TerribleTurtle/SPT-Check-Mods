using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeGitHubReleaseClient : IGitHubReleaseClient
{
    public Func<string, string?>? OnTryGetLatestReleaseAssetUrlAsync;

    public Task<string?> TryGetLatestReleaseAssetUrlAsync(string sourceCodeUrl, CancellationToken token = default)
    {
        return Task.FromResult(OnTryGetLatestReleaseAssetUrlAsync?.Invoke(sourceCodeUrl));
    }

    public Func<string, string, (string? Version, string? Url)>? OnGetLatestReleaseVersionAsync;

    public Task<(string? Version, string? Url)> GetLatestReleaseVersionAsync(string owner, string repo, CancellationToken token = default)
    {
        if (OnGetLatestReleaseVersionAsync != null)
        {
            return Task.FromResult(OnGetLatestReleaseVersionAsync(owner, repo));
        }
        return Task.FromResult<(string? Version, string? Url)>((null, null));
    }
}
