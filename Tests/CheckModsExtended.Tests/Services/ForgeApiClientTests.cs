using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class ForgeApiClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _sendAsync;
        
        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return _sendAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task GetFromJsonAsync_ReturnsData_OnSuccess()
    {
        var body = "{\"Success\":true}";
        var handler = new StubHandler((req, ct) => 
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }));
        var httpClient = new HttpClient(handler);
        var client = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);

        var result = await client.GetFromJsonAsync("https://example.com", CheckModsExtendedJsonSerializerContext.Default.ModApiResponse);

        Assert.True(result.IsT0);
        Assert.NotNull(result.AsT0);
        Assert.True(result.AsT0.Success);
    }

    [Fact]
    public async Task GetFromJsonAsync_ReturnsNotFound_On404()
    {
        var handler = new StubHandler((req, ct) => 
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var httpClient = new HttpClient(handler);
        var client = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);

        var result = await client.GetFromJsonAsync("https://example.com", CheckModsExtendedJsonSerializerContext.Default.ModApiResponse);

        Assert.True(result.IsT1);
    }

    [Fact]
    public async Task GetFromJsonAsync_ReturnsApiError_On500()
    {
        var handler = new StubHandler((req, ct) => 
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var httpClient = new HttpClient(handler);
        var client = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);

        var result = await client.GetFromJsonAsync("https://example.com", CheckModsExtendedJsonSerializerContext.Default.ModApiResponse);

        Assert.True(result.IsT2);
        Assert.Equal(500, result.AsT2.StatusCode);
    }

    [Fact]
    public async Task GetFromJsonAsync_ReturnsApiError_OnNetworkError()
    {
        var handler = new StubHandler((req, ct) => 
            throw new HttpRequestException("Network failure"));
        var httpClient = new HttpClient(handler);
        var client = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);

        var result = await client.GetFromJsonAsync("https://example.com", CheckModsExtendedJsonSerializerContext.Default.ModApiResponse);

        Assert.True(result.IsT2);
        Assert.Contains("Network error", result.AsT2.Message);
        Assert.IsType<HttpRequestException>(result.AsT2.Exception);
    }

    [Fact]
    public async Task GetFromJsonAsync_ReturnsApiError_OnTimeout()
    {
        var handler = new StubHandler((req, ct) => 
            throw new TaskCanceledException("Timeout"));
        var httpClient = new HttpClient(handler);
        var client = new ForgeApiClient(httpClient, NullLogger<ForgeApiClient>.Instance);

        var result = await client.GetFromJsonAsync("https://example.com", CheckModsExtendedJsonSerializerContext.Default.ModApiResponse);

        Assert.True(result.IsT2);
        Assert.Contains("timed out", result.AsT2.Message);
        Assert.IsType<TaskCanceledException>(result.AsT2.Exception);
    }
}
