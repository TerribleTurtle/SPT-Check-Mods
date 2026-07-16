using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using CheckModsExtended.Tests.Fixtures;
using SemanticVersioning;
using Xunit;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Services;

public sealed class CompatibilityValidationServiceTests
{
    private readonly FakeModCheckReporter _reporter = new();
    private readonly CompatibilityValidationService _sut;

    public CompatibilityValidationServiceTests()
    {
        _sut = new CompatibilityValidationService();
    }

    [Fact]
    public void Checkmodversioncompatibility_when_compatible_does_not_flag_incompatibility()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = ModFixture.CreateServerMod("test", "TestMod");

        // Mod has to be matched to be checked
        var apiResult = new ModSearchResult(
            1,
            null,
            "Test Mod",
            "test-mod",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            [new ModVersion(1, null, "1.0.0", null, "url", ">=3.9.0", null, 0, null, null, null)]
        );
        mod = mod.WithApiMatch(apiResult);

        // Act
        mod = _sut.CheckModVersionCompatibility([mod], sptVersion).UpdatedMods[0];

        // Assert
        Assert.False(mod.Update.IsLocalSptIncompatible);
    }

    [Fact]
    public void Checkmodversioncompatibility_when_incompatible_flags_incompatibility_and_suggests_version()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = ModFixture.CreateServerMod("test", "TestMod");

        var apiResult = new ModSearchResult(
            1,
            null,
            "Test Mod",
            "test-mod",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            [
                new ModVersion(1, null, "1.0.0", null, "url", "~3.8.0", null, 0, null, null, null), // Installed but incompatible
                new ModVersion(2, null, "2.0.0", null, "url", "~3.9.0", null, 0, null, null, null), // Latest compatible
            ]
        );
        mod = mod.WithApiMatch(apiResult);

        // Act
        mod = _sut.CheckModVersionCompatibility([mod], sptVersion).UpdatedMods[0];

        // Assert
        Assert.True(mod.Update.IsLocalSptIncompatible);
        Assert.Equal("2.0.0", mod.Update.CompatibleVersionString);
        Assert.Contains("requires SPT ~3.8.0", mod.Update.IncompatibilityReason);
    }

    [Fact]
    public void Checkmodversioncompatibility_warns_when_constraint_unparseable()
    {
        // Arrange
        var sptVersion = new Version("3.9.0");
        var mod = ModFixture.CreateServerMod("test", "TestMod");

        var apiResult = new ModSearchResult(
            1,
            null,
            "Test Mod",
            "test-mod",
            null,
            null,
            0,
            null,
            "url",
            new ModAuthor(1, "Author", null),
            [
                new ModVersion(
                    1,
                    null,
                    "1.0.0",
                    null,
                    "url",
                    "unparseable string that breaks semver",
                    null,
                    0,
                    null,
                    null,
                    null
                ),
            ]
        );
        mod = mod.WithApiMatch(apiResult);

        // Act
        var result = _sut.CheckModVersionCompatibility([mod], sptVersion);
        mod = result.UpdatedMods[0];

        // Assert
        Assert.False(mod.Update.IsLocalSptIncompatible);
        Assert.Contains(result.ValidationEvents, w => w.Contains("invalid version constraint"));
    }
}
