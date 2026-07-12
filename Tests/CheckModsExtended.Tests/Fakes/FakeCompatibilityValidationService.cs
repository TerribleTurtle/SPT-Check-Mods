using System.Collections.Generic;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Tests.Fakes;

public sealed class FakeCompatibilityValidationService : ICompatibilityValidationService
{
    public bool CheckModVersionCompatibilityCalled { get; private set; }

    public (IReadOnlyList<Mod> UpdatedMods, IReadOnlyList<string> ValidationEvents) CheckModVersionCompatibility(
        IEnumerable<Mod> mods,
        Version sptVersion
    )
    {
        CheckModVersionCompatibilityCalled = true;
        return (mods.ToList(), []);
    }
}
