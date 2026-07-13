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
using SemanticVersioning;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class ModUpdateClientTests
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

    private static ModUpdateClient CreateClient(HttpStatusCode status, string body)
    {
        var options = Options.Create(new ForgeApiOptions { BaseUrl = "https://example.test/" });
        var httpClient = new HttpClient(new StubHandler(status, body));
        var apiClient = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);
        return new ModUpdateClient(apiClient, options, NullLogger<ModUpdateClient>.Instance);
    }

    [Fact]
    public async Task GetModUpdatesAsync_ReturnsData_OnSuccess()
    {
        var body = @"{""success"":true,""data"":{""updates"":[{""current_version"":{""mod_id"":1,""version"":""1.0.0""}}]}}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var sptVersion = SemanticVersioning.Version.Parse("3.8.0");
        var mods = new List<(int, string)> { (1, "1.0.0") };
        var result = await client.GetModUpdatesAsync(mods, sptVersion);

        Assert.True(result.IsT0);
        Assert.Single(result.AsT0.SafeToUpdate!);
    }

    [Fact]
    public async Task GetModUpdatesAsync_ReturnsNotFound_WhenNoModsProvided()
    {
        var client = CreateClient(HttpStatusCode.OK, "{}");
        var sptVersion = SemanticVersioning.Version.Parse("3.8.0");
        var mods = new List<(int, string)>();
        var result = await client.GetModUpdatesAsync(mods, sptVersion);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task GetModDependenciesAsync_ReturnsDependencies_OnSuccess()
    {
        var body = @"{""success"":true,""data"":[{""api_mod_id"":1,""identifier"":""test"",""dependencies"":[]}]}";
        var client = CreateClient(HttpStatusCode.OK, body);

        var mods = new List<(string, string)> { ("test", "1.0.0") };
        var result = await client.GetModDependenciesAsync(mods);

        Assert.True(result.IsT0);
        Assert.Single(result.AsT0);
    }

    [Fact]
    public async Task GetModDependenciesAsync_ReturnsNotFound_WhenNoModsProvided()
    {
        var client = CreateClient(HttpStatusCode.OK, "{}");
        var mods = new List<(string, string)>();
        var result = await client.GetModDependenciesAsync(mods);

        Assert.True(result.IsT1);
    }
}

