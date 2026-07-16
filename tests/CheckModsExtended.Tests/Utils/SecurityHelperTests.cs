using System;
using System.IO;
using CheckModsExtended.Utils;
using Xunit;

namespace CheckModsExtended.Tests.Utils;

public class SecurityHelperTests
{
    [Fact]
    public void get_safe_path_returns_null_for_empty_input()
    {
        Assert.Null(SecurityHelper.GetSafePath(null));
        Assert.Null(SecurityHelper.GetSafePath(""));
        Assert.Null(SecurityHelper.GetSafePath("   "));
    }

    [Fact]
    public void get_safe_path_resolves_absolute_path_when_no_base_path()
    {
        var input = "some_folder/test.txt";
        var result = SecurityHelper.GetSafePath(input);
        Assert.NotNull(result);
        Assert.True(Path.IsPathRooted(result));
    }

    [Fact]
    public void get_safe_path_resolves_path_within_base_path()
    {
        var basePath = Environment.CurrentDirectory;
        var input = "test.txt";
        var expected = Path.GetFullPath("test.txt", basePath);
        
        var result = SecurityHelper.GetSafePath(input, basePath);
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void get_safe_path_rejects_directory_traversal_above_base()
    {
        var basePath = Path.Combine(Environment.CurrentDirectory, "AppBase");
        var input = "../outside.txt";
        
        var result = SecurityHelper.GetSafePath(input, basePath);
        
        Assert.Null(result);
    }

    [Fact]
    public void get_safe_path_rejects_absolute_path_outside_base()
    {
        var basePath = Path.Combine(Environment.CurrentDirectory, "AppBase");
        var input = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory)!, "Windows", "System32", "cmd.exe");
        
        var result = SecurityHelper.GetSafePath(input, basePath);
        
        Assert.Null(result);
    }

    [Fact]
    public void get_safe_path_accepts_traversal_that_stays_within_base()
    {
        var basePath = Path.Combine(Environment.CurrentDirectory, "AppBase");
        var input = "subfolder/../test.txt";
        
        var result = SecurityHelper.GetSafePath(input, basePath);
        
        Assert.NotNull(result);
        var expected = Path.GetFullPath(Path.Combine(basePath, "test.txt"));
        Assert.Equal(expected, result);
    }
}
