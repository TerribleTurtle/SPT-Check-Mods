using CheckModsExtended.Models;
using CheckModsExtended.Tests.Fixtures;

namespace CheckModsExtended.Tests.Fixtures;

/// <summary>
/// Centralized factory for creating <see cref="Mod"/> entities in tests.
/// </summary>
public static class ModFixture
{
    public static Mod CreateClientMod(
        string guid,
        string name = "Mod",
        string version = "1.0.0",
        string author = "Author"
    )
    {
        return new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = guid,
                LocalName = name,
                LocalVersion = version,
                LocalAuthor = author,
                FilePath = "",
                IsServerMod = false,
            },
        };
    }

    public static Mod CreateServerMod(
        string guid,
        string name = "Mod",
        string version = "1.0.0",
        string author = "Author"
    )
    {
        return new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = guid,
                LocalName = name,
                LocalVersion = version,
                LocalAuthor = author,
                FilePath = "",
                IsServerMod = true,
            },
        };
    }
}
