using System.Collections.Generic;
using System.Threading.Tasks;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles interactive prompts with the user.
/// </summary>
public interface IUserPromptService
{
    bool PromptFetchRemoteIgnores();
    EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates);
    IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds);
    bool PromptReportIgnores();
    Task<bool> PromptForConfirmationAsync(PendingConfirmation confirmation);
}
