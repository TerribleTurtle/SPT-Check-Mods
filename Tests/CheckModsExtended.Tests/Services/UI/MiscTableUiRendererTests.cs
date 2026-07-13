using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services.UI;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace CheckModsExtended.Tests.Services.UI;

[Collection("ConsoleTests")]

public class MiscTableUiRendererTests
{
    private readonly MiscTableUiRenderer _renderer;

    public MiscTableUiRendererTests()
    {
        _renderer = new MiscTableUiRenderer();
    }

    [Fact]
    public void IgnoredUpdatesList_AppliesFiltersAndSorts_WithoutThrowing()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var ignores = new List<IgnoredUpdate>
        {
            new IgnoredUpdate(1, "1.0", "2.0", "Mod A", null, IgnoreSource.User),
            new IgnoredUpdate(2, "1.1", "2.1", "Mod B", null, IgnoreSource.Remote),
        };

        var options = new ListFilterOptions
        {
            Status = "Remote",
            Search = "Mod B",
            Sort = "name",
            Limit = 1,
        };

        // Act
        _renderer.IgnoredUpdatesList(ignores, options);

        // Assert
        var output = console.Output;
        Assert.Contains("Ignored Updates", output);
        Assert.Contains("Mod B", output);
    }

    [Fact]
    public void InstalledModsList_AppliesFiltersAndSorts_WithoutThrowing()
    {
        // Arrange
        var console = new TestConsole();
        AnsiConsole.Console = console;

        var serverMods = new List<Mod>
        {
            new Mod
            {
                Local = new LocalModIdentity
                {
                    Guid = "guidA",
                    FilePath = "C:/path/A",
                    IsServerMod = true,
                    LocalName = "ServerModA",
                    LocalVersion = "1.0.0",
                    LocalAuthor = "Author"
                },
                Api = new ForgeApiMetadata { ApiName = "Server Mod A" }
            },
        };

        var clientMods = new List<Mod>
        {
            new Mod
            {
                Local = new LocalModIdentity
                {
                    Guid = "guidB",
                    FilePath = "C:/path/B",
                    IsServerMod = false,
                    LocalName = "ClientModB",
                    LocalVersion = "2.0.0",
                    LocalAuthor = "Author"
                },
                Api = new ForgeApiMetadata { ApiName = "Client Mod B" }
            },
        };

        var options = new ListFilterOptions
        {
            Type = "Client",
            Search = "Mod",
            Sort = "name",
            Limit = 5,
            Status = "NoMatch",
        };

        // Act
        _renderer.InstalledModsList(serverMods, clientMods, options);

        // Assert
        var output = console.Output;
        Assert.Contains("Installed Mods", output);
        Assert.Contains("Client Mod B", output);
    }
}
