using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Orchestrates the process of checking for SPT and Check Mods updates,
/// and applying user update suppressions.
/// </summary>
public interface IUpdateOrchestrationService
{
    /// <summary>
    /// Checks for available SPT updates and displays them to the user.
    /// </summary>
    Task CheckForSptUpdatesAsync(Version currentVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a newer version of Check Mods is available on the Forge and displays the result.
    /// </summary>
    Task CheckForCheckModsExtendedUpdateAsync(Version sptVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flags any mod whose available update matches a stored suppression so it renders as ignored.
    /// </summary>
    Task<IReadOnlyList<Mod>> ApplyIgnoredUpdatesAsync(
        IEnumerable<Mod> mods,
        CancellationToken cancellationToken = default
    );
}
