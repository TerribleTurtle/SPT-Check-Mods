using CheckMods.Utils;

namespace CheckMods.Tests;

/// <summary>
/// Tests for <see cref="SemVer"/>, the central version-parsing helper used in place of exception-driven parsing.
/// </summary>
public sealed class SemVerTests
{
    [Fact]
    public void Tryparse_returns_version_for_valid_input_standard()
    {
        Assert.True(SemVer.TryParse("1.2.3", "test").IsT0);
    }

    [Fact]
    public void Tryparse_returns_version_for_valid_input_minor()
    {
        Assert.True(SemVer.TryParse("0.0.1", "test").IsT0);
    }

    [Fact]
    public void Tryparse_returns_version_for_valid_input_prerelease()
    {
        Assert.True(SemVer.TryParse("1.0.0-beta.1", "test").IsT0);
    }

    [Fact]
    public void Tryparse_returns_error_for_missing_or_invalid_input_null()
    {
        Assert.True(SemVer.TryParse(null, "test").IsT1);
    }

    [Fact]
    public void Tryparse_returns_error_for_missing_or_invalid_input_empty()
    {
        Assert.True(SemVer.TryParse("", "test").IsT1);
    }

    [Fact]
    public void Tryparse_returns_error_for_missing_or_invalid_input_whitespace()
    {
        Assert.True(SemVer.TryParse("   ", "test").IsT1);
    }

    [Fact]
    public void Tryparse_returns_error_for_missing_or_invalid_input_text()
    {
        Assert.True(SemVer.TryParse("not-a-version", "test").IsT1);
    }

    [Fact]
    public void Tryparse_returns_error_for_missing_or_invalid_input_abc()
    {
        Assert.True(SemVer.TryParse("abc", "test").IsT1);
    }

    [Fact]
    public void Satisfiesrange_evaluates_constraint_gte_true()
    {
        Assert.True(SemVer.SatisfiesRange(">=1.0.0", new SemanticVersioning.Version("1.2.3")));
    }

    [Fact]
    public void Satisfiesrange_evaluates_constraint_tilde_true()
    {
        Assert.True(SemVer.SatisfiesRange("~1.2.0", new SemanticVersioning.Version("1.2.5")));
    }

    [Fact]
    public void Satisfiesrange_evaluates_constraint_tilde_false()
    {
        Assert.False(SemVer.SatisfiesRange("~1.2.0", new SemanticVersioning.Version("1.3.0")));
    }

    [Fact]
    public void Satisfiesrange_evaluates_constraint_gte_false()
    {
        Assert.False(SemVer.SatisfiesRange(">=2.0.0", new SemanticVersioning.Version("1.9.9")));
    }

    [Fact]
    public void Satisfiesrange_returns_false_for_missing_constraint_null()
    {
        Assert.False(SemVer.SatisfiesRange(null, new SemanticVersioning.Version("1.0.0")));
    }

    [Fact]
    public void Satisfiesrange_returns_false_for_missing_constraint_empty()
    {
        Assert.False(SemVer.SatisfiesRange("", new SemanticVersioning.Version("1.0.0")));
    }
}
