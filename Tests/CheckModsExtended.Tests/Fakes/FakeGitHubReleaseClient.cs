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
}
