using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

public sealed class FakeServerModExtractor : IServerModExtractor
{
    public Mod? ExtractedMod { get; set; }

    public Mod? ExtractServerModMetadata(string dllPath)
    {
        return ExtractedMod;
    }
}






