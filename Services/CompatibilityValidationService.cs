using System;
using System.Collections.Generic;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using SemanticVersioning;
using SPTarkov.DI.Annotations;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Services;

/// <inheritdoc />
[Injectable(InjectionType.Transient)]
public sealed class CompatibilityValidationService : ICompatibilityValidationService
{
    /// <inheritdoc />
    public (IReadOnlyList<Mod> UpdatedMods, IReadOnlyList<string> ValidationEvents) CheckModVersionCompatibility(IEnumerable<Mod> mods, Version sptVersion)
    {
        var updatedMods = new List<Mod>();
        var events = new List<string>();
        foreach (var mod in mods)
        {
            var updatedMod = mod;
            var isCompatible = IsCompatibleWithSpt(updatedMod, sptVersion, events, out var reason, out var compatibleVersion);
            if (!isCompatible)
            {
                updatedMod = updatedMod.WithLocalSptIncompatible(reason, compatibleVersion);
            }
            updatedMods.Add(updatedMod);
        }
        return (updatedMods, events);
    }

    /// <summary>
    /// Evaluates a single matched mod's installed version against the installed SPT version, flagging it when the
    /// constraint can't be parsed or isn't satisfied.
    /// </summary>
    private bool IsCompatibleWithSpt(Mod mod, Version sptVersion, List<string> events, out string reason, out string? compatibleVersion)
    {
        reason = string.Empty;
        compatibleVersion = null;

        if (!mod.IsMatched || mod.Api.ApiVersions is not { Count: > 0 } || mod.Update.UpdateSuppressed)
        {
            return true;
        }

        // Find the version that matches the installed local version.
        var installedApiVersion = mod.Api.ApiVersions!.FirstOrDefault(v =>
            string.Equals(v.Version, mod.Local.LocalVersion, StringComparison.OrdinalIgnoreCase)
        );

        if (installedApiVersion == null)
        {
            // Couldn't find the installed version in the API versions
            return true;
        }

        // Check if the installed version's SPT constraint is compatible with the installed SPT version
        if (string.IsNullOrWhiteSpace(installedApiVersion.SptVersionConstraint))
        {
            return true;
        }

        if (!SemanticVersioning.Range.TryParse(installedApiVersion.SptVersionConstraint, out var range))
        {
            // The constraint from Forge can't be parsed; surface a warning.
            events.Add($"Could not verify SPT compatibility for {mod.DisplayName}: Forge reported an invalid version constraint ({installedApiVersion.SptVersionConstraint}).");
            return true;
        }

        if (range.IsSatisfied(sptVersion))
        {
            // The installed version is compatible - no issue
            return true;
        }

        // The installed version is NOT compatible with the installed SPT version
        reason = $"Version {mod.Local.LocalVersion} requires SPT {installedApiVersion.SptVersionConstraint}";

        // Find the latest compatible version to suggest
        compatibleVersion = mod
            .Api.ApiVersions!.Where(v => SemVer.SatisfiesRange(v.SptVersionConstraint, sptVersion))
            .Select(v => new
            {
                VersionString = v.Version,
                ParsedVersion = SemVer
                    .TryParse(v.Version, "CompatibilityValidationService")
                    .Match(parsed => parsed, _ => new SemanticVersioning.Version(0, 0, 0)),
            })
            .OrderByDescending(x => x.ParsedVersion)
            .FirstOrDefault()
            ?.VersionString;

        return false;
    }
}
