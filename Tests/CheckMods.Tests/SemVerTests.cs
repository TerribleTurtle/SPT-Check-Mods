using CheckMods.Utils;

namespace CheckMods.Tests;

/// <summary>
/// Tests for <see cref="SemVer"/>, the central version-parsing helper used in place of exception-driven parsing.
/// </summary>
public sealed class SemVerTests
{
    [Theory]
    [InlineData("1.2.3")]
    [InlineData("0.0.1")]
    [InlineData("1.0.0-beta.1")]
    public void tryparse_returns_version_for_valid_input(string input)
    {
        Assert.True(SemVer.TryParse(input, "test").IsT0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-version")]
    [InlineData("abc")]
    public void tryparse_returns_error_for_missing_or_invalid_input(string? input)
    {
        Assert.True(SemVer.TryParse(input, "test").IsT1);
    }

    [Theory]
    [InlineData(">=1.0.0", "1.2.3", true)]
    [InlineData("~1.2.0", "1.2.5", true)]
    [InlineData("~1.2.0", "1.3.0", false)]
    [InlineData(">=2.0.0", "1.9.9", false)]
    public void satisfiesrange_evaluates_constraint(string constraint, string version, bool expected)
    {
        Assert.Equal(expected, SemVer.SatisfiesRange(constraint, new SemanticVersioning.Version(version)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void satisfiesrange_returns_false_for_missing_constraint(string? constraint)
    {
        Assert.False(SemVer.SatisfiesRange(constraint, new SemanticVersioning.Version("1.0.0")));
    }
}
