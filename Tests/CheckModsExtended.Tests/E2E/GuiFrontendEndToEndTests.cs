using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
using CheckModsExtended.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace CheckModsExtended.Tests.E2E;

[Collection("Sequential")]
public sealed class GuiFrontendEndToEndTests
{
    [Fact]
    public async Task WebManager_frontend_displays_mods_correctly()
    {
        // Playwright requires browsers to be installed. We will skip the test if they aren't,
        // or just let it fail so CI catches it if they are missing.
        // Usually `playwright install` must be run.

        // Arrange
        var server = WireMockServer.Start();
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            Environment.SetEnvironmentVariable("ForgeApiOptions__BaseUrl", server.Urls[0] + "/");
            Directory.CreateDirectory(tempDir);

            var sptRoot = Path.Combine(tempDir, "SPT");
            Directory.CreateDirectory(sptRoot);
            // 1. Mock the Scanner Directory
            var fakeModDir = Path.Combine(sptRoot, "SPT", "user", "mods", "FakeMod");
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

            // Mock the SPT core DLL so the file exists
            var coreDllPath = Path.Combine(sptRoot, "SPT", "SPTarkov.Server.Core.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(coreDllPath)!);
            File.WriteAllText(coreDllPath, "dummy");

            // Mock the SPT version using the test override file
            var testVersionFile = Path.Combine(sptRoot, "SPT", ".spt_version_test");
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

            var webArgs = new[] { sptRoot };

            var fakeCacheService = new FakeScanCacheService();
            await fakeCacheService.SaveCacheAsync(new ScanCacheRecord(
                DateTimeOffset.UtcNow,
                new ScanResponse(
                    new List<ModDto>
                    {
                        new ModDto(
                            1,
                            "FakeMod",
                            "FakeAuthor",
                            "1.0.0",
                            "1.0.0",
                            "ok",
                            false,
                            null,
                            null
                        )
                    },
                    null,
                    "3.8.0"
                )
            ));

            var runTask = WebManagerHost.RunAsync(webArgs, cts.Token, services =>
            {
                services.AddSingleton<IBrowserLauncher>(launcher);
                services.AddSingleton<IScanCacheService>(fakeCacheService);
            });

            // 4. Wait for the URL
            var urlTask = launcher.WaitForUrlAsync();
            if (await Task.WhenAny(runTask, urlTask) == runTask)
            {
                await runTask; // Throw underlying exception if it crashed
                throw new Exception("WebManagerHost exited prematurely before URL was broadcast.");
            }
            var url = await urlTask;

            // 5. Run Playwright
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

            var page = await browser.NewPageAsync();
            await page.GotoAsync(url);

            // Assert title
            var title = await page.TitleAsync();
            Assert.Contains("CheckModsExtended // MANAGER", title);

            // Wait for the CACHED badge
            var cacheIndicator = page.Locator("#cache-indicator");
            await cacheIndicator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            var cacheText = await cacheIndicator.InnerTextAsync();
            Assert.Contains("CACHED", cacheText);

            // Wait for the toast to indicate auto-scan finished
            var toastContainer = page.Locator("#toast-container");
            await toastContainer.Filter(new LocatorFilterOptions { HasText = "Scan complete." }).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            // Wait for the table to populate with the FakeMod
            var modRow = page.Locator("text='FakeMod'");
            await modRow.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            // Wait for version 1.0.1 (from the mocked API) to appear
            var versionCell = page.Locator("text='v1.0.1'");
            await versionCell.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            // Cleanup
            await browser.CloseAsync();

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
            Environment.SetEnvironmentVariable("ForgeApiOptions__BaseUrl", null);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
