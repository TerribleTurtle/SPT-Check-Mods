using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeServerModExtractor : IServerModExtractor
{
    public Mod? ExtractedMod { get; set; }

    public Task<Mod?> ExtractServerModMetadataAsync(
        string dllPath,
        string sptDirectory,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(ExtractedMod);
    }

    public Task<Mod?> ExtractServerModPackageMetadataAsync(
        string modDirectory,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(ExtractedMod);
    }
}

