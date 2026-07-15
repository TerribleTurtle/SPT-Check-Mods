using System;
using System.Linq;
using CheckModsExtended.Configuration;
using CheckModsExtended.Services.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CheckModsExtended.Tests.Services.Web;

public class WebManagerHostTests
{
    [Fact]
    public void build_app_creates_app_with_headless_config()
    {
        var args = Array.Empty<string>();
        
        var app = WebManagerHost.BuildApp(args);
        
        var config = app.Services.GetRequiredService<RuntimeConfig>();
        Assert.True(config.IsHeadless);
        Assert.False(config.IsVerbose);
        Assert.False(config.IsDebug);
    }

    [Fact]
    public void build_app_sets_verbose_and_debug_from_args()
    {
        var args = new[] { "-v", "--debug" };
        
        var app = WebManagerHost.BuildApp(args);
        
        var config = app.Services.GetRequiredService<RuntimeConfig>();
        Assert.True(config.IsHeadless);
        Assert.True(config.IsVerbose);
        Assert.True(config.IsDebug);
    }
}
