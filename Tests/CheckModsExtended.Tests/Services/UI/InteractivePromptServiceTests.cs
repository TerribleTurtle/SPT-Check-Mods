using System;
using System.Collections.Generic;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.UI;
using Xunit;

namespace CheckModsExtended.Tests.Services.UI;

public class InteractivePromptServiceTests
{
    private InteractivePromptService CreateService()
    {
        var config = new RuntimeConfig { IsHeadless = true };
        return new InteractivePromptService(config);
    }

    private LocalModIdentity CreateDummyLocal()
    {
        return new LocalModIdentity
        {
            Guid = Guid.NewGuid().ToString(),
            FilePath = "test.dll",
            IsServerMod = false,
            LocalName = "Test",
            LocalAuthor = "TestAuthor",
            LocalVersion = "1.0.0"
        };
    }

    [Fact]
    public void prompt_fetch_remote_ignores_returns_false_when_headless()
    {
        var service = CreateService();
        var result = service.PromptFetchRemoteIgnores();
        Assert.False(result);
    }

    [Fact]
    public void prompt_end_of_run_returns_exit_when_headless()
    {
        var service = CreateService();
        var result = service.PromptEndOfRun(5, true);
        Assert.Equal(EndOfRunChoice.Exit, result);
    }

    [Fact]
    public void select_updates_to_ignore_returns_pre_ignored_when_headless()
    {
        var service = CreateService();
        var candidates = new List<Mod>
        {
            new Mod { Local = CreateDummyLocal(), Api = new ForgeApiMetadata { ApiModId = 1 } },
            new Mod { Local = CreateDummyLocal(), Api = new ForgeApiMetadata { ApiModId = 2 } },
            new Mod { Local = CreateDummyLocal(), Api = new ForgeApiMetadata { ApiModId = 3 } }
        };
        var preIgnored = new HashSet<int> { 1, 3 };

        var result = service.SelectUpdatesToIgnore(candidates, preIgnored);

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Api.ApiModId);
        Assert.Equal(3, result[1].Api.ApiModId);
    }

    [Fact]
    public void prompt_report_ignores_returns_false_when_headless()
    {
        var service = CreateService();
        var result = service.PromptReportIgnores();
        Assert.False(result);
    }

    [Fact]
    public void prompt_load_from_cache_returns_false_when_headless()
    {
        var service = CreateService();
        var result = service.PromptLoadFromCache(DateTimeOffset.Now);
        Assert.False(result);
    }
}
