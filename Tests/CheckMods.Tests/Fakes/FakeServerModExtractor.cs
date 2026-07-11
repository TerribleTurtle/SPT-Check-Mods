using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

public sealed class FakeServerModExtractor : IServerModExtractor
{
    public Mod? ExtractedMod { get; set; }

    public Task<Mod?> ExtractServerModMetadataAsync(string dllPath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ExtractedMod);
    }
}
