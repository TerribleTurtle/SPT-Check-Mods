using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using Version = SemanticVersioning.Version;
using SemanticVersioning;

namespace CheckModsExtended.Services;

/// <inheritdoc />
[Injectable(InjectionType.Transient)]
public sealed class UpdateOrchestrationService(
    ISptInstallationService sptInstallationService,
    IUpdateCheckService updateCheckService,
    IIgnoredUpdateStore ignoredUpdateStore,
    IModCheckReporter reporter
) : IUpdateOrchestrationService
{
    /// <inheritdoc />
    public async Task CheckForSptUpdatesAsync(Version currentVersion, CancellationToken cancellationToken = default)
    {
        reporter.Blank();
        reporter.Status("Checking for SPT updates...");

        var availableUpdates = await sptInstallationService.CheckForSptUpdatesAsync(currentVersion, cancellationToken);

        if (availableUpdates.Count == 0)
        {
            reporter.Success("You are running the latest version of SPT!");
            return;
        }

        // Show only the latest available update
        reporter.SptUpdateAvailable(availableUpdates[0]);
    }

    /// <inheritdoc />
    public async Task CheckForCheckModsExtendedUpdateAsync(
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        reporter.Heading("Checking for Check Mods updates...");

        var result = await updateCheckService.CheckAsync(sptVersion, cancellationToken);
        reporter.CheckModsExtendedUpdate(result, sptVersion);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Mod>> ApplyIgnoredUpdatesAsync(
        IEnumerable<Mod> mods,
        CancellationToken cancellationToken = default
    )
    {
        var modsList = mods.ToList();
        var updatedMods = new List<Mod>();
        foreach (var mod in modsList)
        {
            var updatedMod = mod;
            if (
                mod.Update.UpdateStatus == UpdateStatus.UpdateAvailable
                && await ignoredUpdateStore.IsIgnoredAsync(mod, cancellationToken)
            )
            {
                updatedMod = updatedMod.WithUpdateSuppressed(true);
            }
            updatedMods.Add(updatedMod);
        }
        return updatedMods;
    }
}
