using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using Version = SemanticVersioning.Version;

namespace CheckMods.Services;

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
    public async Task CheckForSptUpdatesAsync(
        Version currentVersion,
        CancellationToken cancellationToken = default
    )
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
    public async Task CheckForCheckModsUpdateAsync(
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        reporter.Heading("Checking for Check Mods updates...");

        var result = await updateCheckService.CheckAsync(sptVersion, cancellationToken);
        reporter.CheckModsUpdate(result, sptVersion);
    }

    /// <inheritdoc />
    public void ApplyIgnoredUpdates(List<Mod> mods)
    {
        foreach (var mod in mods)
        {
            if (mod.Update.UpdateStatus == UpdateStatus.UpdateAvailable && ignoredUpdateStore.IsIgnored(mod))
            {
                mod.SetUpdateSuppressed(true);
            }
        }
    }
}
