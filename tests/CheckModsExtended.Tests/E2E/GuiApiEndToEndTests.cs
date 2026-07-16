using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
using CheckModsExtended.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace CheckModsExtended.Tests.E2E;

[Collection("Sequential")]
public sealed class GuiApiEndToEndTests
{
    [Fact]
    public async Task WebManager_scan_endpoint_returns_expected_json()
    {
        // Arrange
        var server = WireMockServer.Start();
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var sptRoot = Path.Combine(tempDir, "SPT");

        var originalAppDir = Environment.GetEnvironmentVariable("AppPaths__AppDataDirectory");
        var originalForgeApiUrl = Environment.GetEnvironmentVariable("ForgeApiOptions__BaseUrl");

        Environment.SetEnvironmentVariable("AppPaths__AppDataDirectory", tempDir);

        try
        {
            Environment.SetEnvironmentVariable("ForgeApiOptions__BaseUrl", server.Urls[0] + "/");
            Directory.CreateDirectory(tempDir);

            Directory.CreateDirectory(sptRoot);
            // 1. Mock the Scanner Directory
            var fakeModDir = Path.Combine(sptRoot, "user", "mods", "FakeMod");
            Directory.CreateDirectory(fakeModDir);

            var packageJsonPath = Path.Combine(fakeModDir, "package.json");
            File.WriteAllText(
                packageJsonPath,
                """
                {
                  "name": "FakeMod",
                  "version": "1.0.0",
                  "author": "FakeAuthor"
                }
                """
            );

            // SPT version file for SptInstallationService to detect
            var sptVersionPath = Path.Combine(sptRoot, "Aki_Data", "Server", "configs", "core.json");
            Directory.CreateDirectory(Path.GetDirectoryName(sptVersionPath)!);
            File.WriteAllText(
                sptVersionPath,
                """
                {
                  "akiVersion": "3.8.0"
                }
                """
            );

            // Mock the SPT core DLL so the file exists
            var coreDllPath = Path.Combine(sptRoot, "SPTarkov.Server.Core.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(coreDllPath)!);
            File.WriteAllText(coreDllPath, "dummy");

            // Mock the SPT version using the test override file
            var testVersionFile = Path.Combine(sptRoot, ".spt_version_test");
            File.WriteAllText(testVersionFile, "3.8.0");

            // 2. Mock Forge API Endpoints
            server
                .Given(Request.Create().WithPath("/spt/versions").UsingGet())
                .RespondWith(
                    Response
                        .Create()
                        .WithStatusCode(200)
                        .WithBody(
                            $$"""
                            {
                              "success": true,
                              "data": [
                                {
                                  "id": 1,
                                  "version": "3.8.0",
                                  "version_major": 3,
                                  "version_minor": 8,
                                  "version_patch": 0,
                                  "version_labels": "",
                                  "mod_count": 1
                                }
                              ]
                            }
                            """
                        )
                );

            server
                .Given(Request.Create().WithPath("/mods").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(
                    """
                    {
                      "success": true,
                      "data": [
                        {
                          "id": 1,
                          "name": "FakeMod",
                          "slug": "fakemod",
                          "downloads": 0,
                          "owner": {
                            "id": 1,
                            "name": "FakeAuthor"
                          },
                          "versions": [
                            {
                              "id": 1,
                              "mod_id": 1,
                              "version": "1.0.0",
                              "spt_versions": ["3.8.0"]
                            }
                          ]
                        }
                      ]
                    }
                    """
                ));

            server
                .Given(Request.Create().WithPath("/mods/updates").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(
                    """
                    {
                      "success": true,
                      "data": {
                        "updates": [
                          {
                            "current_version": {
                              "mod_id": 1,
                              "version": "1.0.0"
                            },
                            "recommended_version": {
                              "mod_id": 1,
                              "version": "1.0.1"
                            }
                          }
                        ]
                      }
                    }
                    """
                ));

            server
                .Given(Request.Create().WithPath("/mods/dependencies").UsingGet())
                .RespondWith(Response.Create().WithStatusCode(200).WithBody(
                    """
                    {
                      "success": true,
                      "data": []
                    }
                    """
                ));

            // 3. Start Web Manager
            var launcher = new TestBrowserLauncher();
            using var cts = new CancellationTokenSource();

            // Program.Main strips "gui", so we pass what WebManagerHost expects
            var webArgs = new[] { sptRoot };

            var runTask = WebManagerHost.RunAsync(webArgs, cts.Token, services =>
            {
                services.AddSingleton<IBrowserLauncher>(launcher);
            });

            // 4. Wait for the URL
            var urlTask = launcher.WaitForUrlAsync();
            if (await Task.WhenAny(runTask, urlTask) == runTask)
            {
                await runTask; // Throw underlying exception if it crashed
                throw new Exception("WebManagerHost exited prematurely before URL was broadcast.");
            }
            var url = await urlTask;
            using var client = new HttpClient();

            // Act - Test Status
            var statusRes = await client.GetAsync($"{url}/api/status");
            statusRes.EnsureSuccessStatusCode();
            var statusContent = await statusRes.Content.ReadAsStringAsync();
            Assert.Contains("running", statusContent);

            // Act - Test Scan
            var scanRes = await client.PostAsync($"{url}/api/scan", null);
            scanRes.EnsureSuccessStatusCode();
            var scanContent = await scanRes.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("FakeMod", scanContent);
            Assert.Contains("1.0.1", scanContent);

            // Cleanup
            cts.Cancel();
            try
            {
                await runTask;
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
        }
        finally
        {
            server.Stop();
            server.Dispose();
            Environment.SetEnvironmentVariable("ForgeApiOptions__BaseUrl", originalForgeApiUrl);
            Environment.SetEnvironmentVariable("AppPaths__AppDataDirectory", originalAppDir);

            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (IOException) { }
            }
        }
    }
}
