using CheckModsExtended.Utils;

namespace CheckModsExtended.Tests.Utils;

/// <summary>
/// Tests for <see cref="ModNameNormalizer"/>, which underpins name-based mod matching.
/// </summary>
public sealed class ModNameNormalizerTests
{
    [Fact]
    public void Normalize_strips_separators_and_lowercases_dash_underscore()
    {
        Assert.Equal("mymodname", ModNameNormalizer.Normalize("My-Mod_Name"));
    }

    [Fact]
    public void Normalize_strips_separators_and_lowercases_space_dot()
    {
        Assert.Equal("mymodname", ModNameNormalizer.Normalize("My Mod.Name"));
    }

    [Fact]
    public void Normalize_strips_separators_and_lowercases_acronym()
    {
        Assert.Equal("svm", ModNameNormalizer.Normalize("SVM"));
    }

    [Fact]
    public void Normalize_removes_component_suffix_when_requested_server()
    {
        Assert.Equal("mymod", ModNameNormalizer.Normalize("MyModServer", removeComponentSuffixes: true));
    }

    [Fact]
    public void Normalize_removes_component_suffix_when_requested_client()
    {
        Assert.Equal("mymod", ModNameNormalizer.Normalize("MyModClient", removeComponentSuffixes: true));
    }

    [Fact]
    public void Normalize_returns_empty_for_missing_input_null()
    {
        Assert.Equal(string.Empty, ModNameNormalizer.Normalize(null));
    }

    [Fact]
    public void Normalize_returns_empty_for_missing_input_whitespace()
    {
        Assert.Equal(string.Empty, ModNameNormalizer.Normalize("   "));
    }

    [Fact]
    public void Extractnamefromguid_returns_last_segment_normal()
    {
        Assert.Equal("modname", ModNameNormalizer.ExtractNameFromGuid("com.author.modname"));
    }

    [Fact]
    public void Extractnamefromguid_returns_last_segment_hyphen()
    {
        Assert.Equal("name", ModNameNormalizer.ExtractNameFromGuid("com.author.mod-name"));
    }

    [Fact]
    public void Extractnamefromguid_returns_last_segment_empty()
    {
        Assert.Equal("", ModNameNormalizer.ExtractNameFromGuid(""));
    }

    [Fact]
    public void Isexactmatch_matches_after_normalization()
    {
        Assert.True(ModNameNormalizer.IsExactMatch("My-Mod", "my mod"));
        Assert.False(ModNameNormalizer.IsExactMatch("ModA", "ModB"));
    }

    [Fact]
    public void Isexactmatch_with_suffix_removal_matches_server_and_client()
    {
        Assert.True(ModNameNormalizer.IsExactMatch("CoolModServer", "CoolModClient", removeComponentSuffixes: true));
    }

    [Fact]
    public void Normalize_removes_backend_suffix()
    {
        Assert.Equal("mymod", ModNameNormalizer.Normalize("MyModBackend", removeComponentSuffixes: true));
    }

    [Fact]
    public void Normalize_removes_frontend_suffix()
    {
        Assert.Equal("mymod", ModNameNormalizer.Normalize("MyModFrontend", removeComponentSuffixes: true));
    }

    [Fact]
    public void Getfuzzymatchscore_is_100_for_identical_normalized_names()
    {
        Assert.Equal(100, ModNameNormalizer.GetFuzzyMatchScore("My Mod", "my-mod"));
    }
}
