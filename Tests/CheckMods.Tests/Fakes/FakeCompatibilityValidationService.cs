using System.Collections.Generic;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

public sealed class FakeCompatibilityValidationService : ICompatibilityValidationService
{
    public bool CheckModVersionCompatibilityCalled { get; private set; }

    public void CheckModVersionCompatibility(List<Mod> mods, Version sptVersion)
    {
        CheckModVersionCompatibilityCalled = true;
    }
}
