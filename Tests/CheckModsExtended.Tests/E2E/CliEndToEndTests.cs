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
public class CliEndToEndTests
{
    [Fact]
    public async Task Program_main_when_given_valid_args_returns_success_exit_code()
    {
        // Arrange
        var server = WireMockServer.Start();
        Environment.SetEnvironmentVariable("ForgeApi__BaseUrl", server.Urls[0] + "/");

        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var sptRoot = Path.Combine(tempDir, "SPT");
        Directory.CreateDirectory(sptRoot);

        try
        {
            // 1. Mock the Scanner Directory
            var fakeModDir = Path.Combine(sptRoot, "user", "mods", "FakeMod");
            Directory.CreateDirectory(fakeModDir);

            var packageJsonPath = Path.Combine(fakeModDir, "package.json");
            File.WriteAllText(packageJsonPath, """
            {
              "name": "FakeMod",
              "version": "1.0.0",
              "author": "FakeAuthor"
            }
            """);

            // SPT version file for SptInstallationService to detect
            var sptVersionPath = Path.Combine(sptRoot, "Aki_Data", "Server", "configs", "core.json");
            Directory.CreateDirectory(Path.GetDirectoryName(sptVersionPath)!);
            File.WriteAllText(sptVersionPath, """
            {
              "akiVersion": "3.8.0"
            }
            """);

            // 2. Mock Forge API Endpoints
            server.Given(
                Request.Create().WithPath("/spt/versions").UsingGet()
            ).RespondWith(
                Response.Create().WithStatusCode(200).WithBody("""
                [
                  "3.8.0"
                ]
                """)
            );

            server.Given(
                Request.Create().WithPath("/mods").UsingGet()
            ).RespondWith(
                Response.Create().WithStatusCode(200).WithBody("""
                [
                  {
                    "id": "1",
                    "name": "FakeMod",
                    "author": "FakeAuthor",
                    "versions": [
                      {
                        "version": "1.0.0",
                        "sptVersion": "3.8.0"
                      }
                    ]
                  }
                ]
                """)
            );

            server.Given(
                Request.Create().WithPath("/mods/updates").UsingGet()
            ).RespondWith(
                Response.Create().WithStatusCode(200).WithBody("""
                [
                  {
                    "modId": "1",
                    "name": "FakeMod",
                    "currentVersion": "1.0.0",
                    "latestVersion": "1.0.1",
                    "latestSptVersion": "3.8.0",
                    "updateAvailable": true
                  }
                ]
                """)
            );

            server.Given(
                Request.Create().WithPath("/mods/dependencies").UsingGet()
            ).RespondWith(
                Response.Create().WithStatusCode(200).WithBody("""
                []
                """)
            );

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
            Environment.SetEnvironmentVariable("ForgeApi__BaseUrl", null);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
