using System;
using System.Collections.Generic;
using System.Linq;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using SPTarkov.DI.Annotations;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Services;

/// <inheritdoc />
[Injectable(InjectionType.Transient)]
public sealed class CompatibilityValidationService(IModCheckReporter reporter) : ICompatibilityValidationService
{
    /// <inheritdoc />
    public void CheckModVersionCompatibility(List<Mod> mods, Version sptVersion)
    {
        // Only check mods that are matched with the API and have versions stored, skipping those whose update was
        // dismissed as a false positive.
        var matchedMods = mods.Where(m => m.IsMatched && m.Api.ApiVersions is { Count: > 0 } && !m.Update.UpdateSuppressed)
            .ToList();

        foreach (var mod in matchedMods)
        {
            CheckModSptCompatibility(mod, sptVersion);
        }
    }

    /// <summary>
    /// Evaluates a single matched mod's installed version against the installed SPT version, flagging it when the
    /// constraint can't be parsed or isn't satisfied.
    /// </summary>
    private void CheckModSptCompatibility(Mod mod, Version sptVersion)
    {
        // Find the version that matches the installed local version.
        var installedApiVersion = mod.Api.ApiVersions!.FirstOrDefault(v =>
            string.Equals(v.Version, mod.Local.LocalVersion, StringComparison.OrdinalIgnoreCase)
        );

        if (installedApiVersion == null)
        {
            // Couldn't find the installed version in the API versions
            return;
        }

        // Check if the installed version's SPT constraint is compatible with the installed SPT version
        if (string.IsNullOrWhiteSpace(installedApiVersion.SptVersionConstraint))
        {
            return;
        }

        if (!SemanticVersioning.Range.TryParse(installedApiVersion.SptVersionConstraint, out var range))
        {
            // The constraint from Forge can't be parsed; surface a warning.
            reporter.Warning(
                $"Could not verify SPT compatibility for {mod.DisplayName}: Forge reported an invalid version constraint ({installedApiVersion.SptVersionConstraint})."
            );
            return;
        }

        if (range.IsSatisfied(sptVersion))
        {
            // The installed version is compatible - no issue
            return;
        }

        // The installed version is NOT compatible with the installed SPT version
        var reason = $"Version {mod.Local.LocalVersion} requires SPT {installedApiVersion.SptVersionConstraint}";

        // Find the latest compatible version to suggest
        var compatibleApiVersion = mod.Api.ApiVersions!.Where(v =>
                SemVer.SatisfiesRange(v.SptVersionConstraint, sptVersion)
            )
            .OrderByDescending(v => (SemVer.TryParse(v.Version, "CompatibilityValidationService").Match(v => v, _ => new SemanticVersioning.Version(0, 0, 0))))
            .FirstOrDefault();

        mod.SetLocalSptIncompatible(reason, compatibleApiVersion?.Version);
    }
}
