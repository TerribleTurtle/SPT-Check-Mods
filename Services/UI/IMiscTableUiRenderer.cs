using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.UI;

/// <summary>
/// Renders miscellaneous tables like ignored updates and installed mods.
/// </summary>
public interface IMiscTableUiRenderer
{
    void PendingConfirmationsSummary(IReadOnlyList<PendingConfirmation> pendingConfirmations);
    void IgnoredUpdatesList(IReadOnlyList<IgnoredUpdate> ignores, ListFilterOptions? options = null);
    void InstalledModsList(
        IReadOnlyList<Mod> serverMods,
        IReadOnlyList<Mod> clientMods,
        ListFilterOptions? options = null
    );
}
