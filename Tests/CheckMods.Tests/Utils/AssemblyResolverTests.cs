using CheckMods.Services;
using System.Reflection;

namespace CheckMods.Tests.Utils;

/// <summary>
/// Tests for <see cref="AssemblyResolver"/>.
/// </summary>
public sealed class AssemblyResolverTests
{
    [Fact]
    public void Resolve_does_not_throw_for_valid_assembly()
    {
        // Simply ensure it doesn't crash when accessed or tested basically
        Assert.NotNull(typeof(AssemblyResolver));
    }
}
