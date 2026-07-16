using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public class FakeCacheManager : ICacheManager
{
    public int ClearCallCount { get; private set; }

    public void Clear()
    {
        ClearCallCount++;
    }
}
