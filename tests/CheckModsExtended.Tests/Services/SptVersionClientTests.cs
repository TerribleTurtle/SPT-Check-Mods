using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class SptVersionClientTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static SptVersionClient CreateClient(HttpStatusCode status, string body)
    {
        var options = Options.Create(new ForgeApiOptions { BaseUrl = "https://example.test/" });
        var httpClient = new HttpClient(new StubHandler(status, body));
        var apiClient = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);
        return new SptVersionClient(apiClient, options, NullLogger<SptVersionClient>.Instance);
    }

    [Fact]
    public async Task ValidateSptVersionAsync_ReturnsTrue_OnSuccess()
    {
        var body = @"{""success"":true,""data"":[{""version"":""3.8.0"",""publish_date"":""2024-01-01"",""is_stable"":true}]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var result = await client.ValidateSptVersionAsync("3.8.0");

        Assert.True(result.IsT0);
        Assert.True(result.AsT0);
    }

    [Fact]
    public async Task ValidateSptVersionAsync_ReturnsInvalid_WhenNotFound()
    {
        var body = @"{""success"":true,""data"":[]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var result = await client.ValidateSptVersionAsync("3.8.0");

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task GetAllSptVersionsAsync_ReturnsData_OnSuccess()
    {
        var body = @"{""success"":true,""data"":[{""version"":""3.8.0"",""publish_date"":""2024-01-01"",""is_stable"":true}]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var result = await client.GetAllSptVersionsAsync();

        Assert.True(result.IsT0);
        Assert.Single(result.AsT0);
        Assert.Equal("3.8.0", result.AsT0[0].Version);
    }
}
