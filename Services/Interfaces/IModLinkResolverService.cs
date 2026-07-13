using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

public interface IModLinkResolverService
{
    string? ResolveUpToDateLink(Mod mod, UpToDateMod upToDate);
    string? ResolveIncompatibleLink(Mod mod, IncompatibleMod incompatible);
}
