using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
using CheckModsExtended.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CheckModsExtended.Tests.Services.Web;

[Collection("Sequential")]
public class WebEndpointsTests
{
    public record OpenSystemRequest(string Target);

    [Fact]
    public async Task Get_Cache_WithValidCache_ReturnsOk()
    {
        var cacheService = new FakeScanCacheService();
        var cacheRecord = new ScanCacheRecord(System.TimeProvider.System.GetUtcNow(), new ScanResponse(new List<ModDto>(), null, null));
        await cacheService.SaveCacheAsync(cacheRecord);

        var launcher = new TestBrowserLauncher();
        using var cts = new CancellationTokenSource();
        var runTask = WebManagerHost.RunAsync(new string[0], cts.Token, services =>
        {
            services.AddSingleton<IScanCacheService>(cacheService);
            services.AddSingleton<IBrowserLauncher>(launcher);
        });

        var url = await launcher.WaitForUrlAsync();
        using var client = new HttpClient();
        var response = await client.GetAsync($"{url}/api/cache");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        cts.Cancel();
        try { await runTask; } catch { }
    }

    [Fact]
    public async Task Get_Cache_WithNoCache_ReturnsNotFound()
    {
        var cacheService = new FakeScanCacheService();
        var launcher = new TestBrowserLauncher();
        using var cts = new CancellationTokenSource();
        var runTask = WebManagerHost.RunAsync(new string[0], cts.Token, services =>
        {
            services.AddSingleton<IScanCacheService>(cacheService);
            services.AddSingleton<IBrowserLauncher>(launcher);
        });

        var url = await launcher.WaitForUrlAsync();
        using var client = new HttpClient();
        var response = await client.GetAsync($"{url}/api/cache");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        cts.Cancel();
        try { await runTask; } catch { }
    }

    [Fact]
    public async Task Post_SystemOpen_WithInvalidTarget_ReturnsBadRequest()
    {
        var launcher = new TestBrowserLauncher();
        using var cts = new CancellationTokenSource();
        var runTask = WebManagerHost.RunAsync(new string[0], cts.Token, services =>
        {
            services.AddSingleton<IBrowserLauncher>(launcher);
        });

        var urlTask = launcher.WaitForUrlAsync();
        if (await Task.WhenAny(runTask, urlTask) == runTask)
        {
            await runTask;
            throw new System.Exception("WebManagerHost exited prematurely.");
        }
        var url = await urlTask;
        using var client = new HttpClient();

        var request = new OpenSystemRequest("|||invalid-target-that-will-throw-win32-exception|||");
        var response = await client.PostAsJsonAsync($"{url}/api/system/open", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        cts.Cancel();
        try { await runTask; } catch { }
    }
}

