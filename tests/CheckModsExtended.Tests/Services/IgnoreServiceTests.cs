using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public class IgnoreServiceTests
{
    [Fact]
    public async Task AddIgnoreAsync_NewIgnore_ReturnsTrueAndSaves()
    {
        var store = new FakeIgnoredUpdateStore();
        var service = new IgnoreService(store);

        var result = await service.AddIgnoreAsync(123, "1.0.0", "1.1.0");

        Assert.True(result);
        var ignores = await store.LoadAsync();
        Assert.Single(ignores);
        Assert.Equal(123, ignores.First().ApiModId);
    }

    [Fact]
    public async Task AddIgnoreAsync_ExistingIgnore_ReturnsFalseAndDoesNotDuplicate()
    {
        var store = new FakeIgnoredUpdateStore();
        await store.SaveAsync(new[]
        {
            new IgnoredUpdate(123, "1.0.0", "1.1.0", Source: IgnoreSource.User, DismissedUtc: DateTimeOffset.UtcNow)
        });
        var service = new IgnoreService(store);

        var result = await service.AddIgnoreAsync(123, "1.0.0", "1.1.0");

        Assert.False(result);
        var ignores = await store.LoadAsync();
        Assert.Single(ignores);
    }

    [Fact]
    public async Task RemoveIgnoreAsync_ExistingIgnore_ReturnsCountAndRemoves()
    {
        var store = new FakeIgnoredUpdateStore();
        await store.SaveAsync(new[]
        {
            new IgnoredUpdate(123, "1.0.0", "1.1.0", Source: IgnoreSource.User, DismissedUtc: DateTimeOffset.UtcNow),
            new IgnoredUpdate(456, "2.0.0", "2.1.0", Source: IgnoreSource.User, DismissedUtc: DateTimeOffset.UtcNow)
        });
        var service = new IgnoreService(store);

        var result = await service.RemoveIgnoreAsync(123);

        Assert.Equal(1, result);
        var ignores = await store.LoadAsync();
        Assert.Single(ignores);
        Assert.Equal(456, ignores.First().ApiModId);
    }

    [Fact]
    public async Task GetIgnoresAsync_ReturnsIgnoresFromStore()
    {
        var store = new FakeIgnoredUpdateStore();
        await store.SaveAsync(new[]
        {
            new IgnoredUpdate(123, "1.0.0", "1.1.0", Source: IgnoreSource.User, DismissedUtc: DateTimeOffset.UtcNow)
        });
        var service = new IgnoreService(store);

        var result = await service.GetIgnoresAsync();

        Assert.Single(result);
        Assert.Equal(123, result[0].ApiModId);
    }
}
