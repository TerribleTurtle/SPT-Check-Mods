using System;
using System.IO;
using System.Threading.Tasks;
using CheckModsExtended;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace CheckModsExtended.Tests.E2E;

[Collection("Sequential")]
public sealed class CliEndToEndTests
{
    [Fact]
    public async Task Program_main_when_given_valid_args_returns_success_exit_code()
    {
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

            // Act
            // Provide the mocked sptRoot as the argument and use -y for headless
            var exitCode = await Program.Main(new[] { sptRoot, "-y" });

            // Assert
            Assert.Equal(ExitCodes.Success, exitCode);
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
