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

public class WebEndpointsTests
{
    public record OpenSystemRequest(string Target);

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

