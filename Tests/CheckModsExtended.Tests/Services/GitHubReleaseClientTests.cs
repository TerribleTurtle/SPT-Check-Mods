using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class GitHubReleaseClientTests
{
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage>? Handler { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (Handler != null)
            {
                return Task.FromResult(Handler(request));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task TryGetLatestReleaseAssetUrlAsync_WithValidRelease_ReturnsAssetUrl()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                    "assets": [
                        {
                            "name": "release.zip",
                            "browser_download_url": "https://github.com/owner/repo/releases/download/1.0/release.zip"
                        }
                    ]
                }
                """)
            }
        };
        var client = new GitHubReleaseClient(new HttpClient(handler), new FakeLogger<GitHubReleaseClient>());

        // Act
        var result = await client.TryGetLatestReleaseAssetUrlAsync("https://github.com/owner/repo");

        // Assert
        Assert.Equal("https://github.com/owner/repo/releases/download/1.0/release.zip", result);
    }

    [Fact]
    public async Task TryGetLatestReleaseAssetUrlAsync_WithHttpError_ReturnsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        var client = new GitHubReleaseClient(new HttpClient(handler), new FakeLogger<GitHubReleaseClient>());

        // Act
        var result = await client.TryGetLatestReleaseAssetUrlAsync("https://github.com/owner/repo");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetLatestReleaseAssetUrlAsync_WithForbidden_ReturnsNull()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.Forbidden)
        };
        var client = new GitHubReleaseClient(new HttpClient(handler), new FakeLogger<GitHubReleaseClient>());

        // Act
        var result = await client.TryGetLatestReleaseAssetUrlAsync("https://github.com/owner/repo");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetLatestReleaseAssetUrlAsync_WithNetworkError_ReturnsNullAndCatchesHttpRequestException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Handler = req => throw new HttpRequestException("Network error")
        };
        var client = new GitHubReleaseClient(new HttpClient(handler), new FakeLogger<GitHubReleaseClient>());

        // Act
        var result = await client.TryGetLatestReleaseAssetUrlAsync("https://github.com/owner/repo");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetLatestReleaseAssetUrlAsync_WithMalformedJson_ReturnsNullAndCatchesJsonException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("invalid json")
            }
        };
        var client = new GitHubReleaseClient(new HttpClient(handler), new FakeLogger<GitHubReleaseClient>());

        // Act
        var result = await client.TryGetLatestReleaseAssetUrlAsync("https://github.com/owner/repo");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetLatestReleaseAssetUrlAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler
        {
            Handler = req => throw new OperationCanceledException()
        };
        var client = new GitHubReleaseClient(new HttpClient(handler), new FakeLogger<GitHubReleaseClient>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            client.TryGetLatestReleaseAssetUrlAsync("https://github.com/owner/repo", cts.Token));
    }
}
