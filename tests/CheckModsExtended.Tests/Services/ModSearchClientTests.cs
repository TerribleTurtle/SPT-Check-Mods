using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SemanticVersioning;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class ModSearchClientTests
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

    private static ModSearchClient CreateClient(HttpStatusCode status, string body)
    {
        var options = Options.Create(new ForgeApiOptions { BaseUrl = "https://example.test/" });
        var httpClient = new HttpClient(new StubHandler(status, body));
        var apiClient = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);
        return new ModSearchClient(apiClient, options, NullLogger<ModSearchClient>.Instance);
    }

    [Fact]
    public async Task SearchModsAsync_ReturnsMods_OnSuccess()
    {
        var body = @"{""success"":true,""data"":[{""name"":""TestMod"",""slug"":""test-slug"",""versions"":[]}]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var sptVersion = SemanticVersioning.Version.Parse("3.8.0");
        var result = await client.SearchModsAsync("TestMod", sptVersion);

        Assert.True(result.IsT0);
        Assert.Single(result.AsT0);
        Assert.Equal("test-slug", result.AsT0[0].Slug);
    }

    [Fact]
    public async Task SearchClientModsAsync_ReturnsMods_OnSuccess()
    {
        var body = @"{""success"":true,""data"":[{""name"":""TestMod"",""slug"":""test-slug"",""versions"":[]}]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var sptVersion = SemanticVersioning.Version.Parse("3.8.0");
        var result = await client.SearchClientModsAsync("TestMod", sptVersion);

        Assert.True(result.IsT0);
        Assert.Single(result.AsT0);
    }

    [Fact]
    public async Task GetModByIdAsync_ReturnsMod_OnSuccess()
    {
        var body = @"{""success"":true,""data"":{""name"":""TestMod"",""slug"":""test-slug"",""versions"":[]}}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var result = await client.GetModByIdAsync(1);

        Assert.True(result.IsT0);
        Assert.Equal("test-slug", result.AsT0.Slug);
    }

    [Fact]
    public async Task GetModByIdAsync_ReturnsInvalidInput_OnInvalidId()
    {
        var client = CreateClient(HttpStatusCode.OK, "{}");
        var result = await client.GetModByIdAsync(0);

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task GetModByGuidAsync_ReturnsMod_OnSuccess()
    {
        var body = @"{""success"":true,""data"":[{""name"":""TestMod"",""slug"":""test-slug"",""versions"":[{""version"":""1.0.0"", ""spt_version_constraint"":""3.8.0""}]}]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var sptVersion = SemanticVersioning.Version.Parse("3.8.0");
        var result = await client.GetModByGuidAsync("test-slug", sptVersion);

        Assert.True(result.IsT0);
        Assert.Equal("test-slug", result.AsT0.Slug);
    }

    [Fact]
    public async Task GetModByGuidAsync_ReturnsNoCompatibleVersion()
    {
        var body = @"{""success"":true,""data"":[{""name"":""TestMod"",""slug"":""test-slug"",""versions"":[{""version"":""1.0.0"", ""spt_version"":""3.9.0""}]}]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var sptVersion = SemanticVersioning.Version.Parse("3.8.0");
        var result = await client.GetModByGuidAsync("test-slug", sptVersion);

        Assert.True(result.IsT2);
    }

    [Fact]
    public async Task GetModByGuidAsync_ReturnsNotFound_WhenEmpty()
    {
        var body = @"{""success"":true,""data"":[]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var sptVersion = SemanticVersioning.Version.Parse("3.8.0");
        var result = await client.GetModByGuidAsync("test-slug", sptVersion);

        Assert.True(result.IsT1);
    }
}

