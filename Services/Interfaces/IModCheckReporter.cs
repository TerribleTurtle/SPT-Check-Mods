using CheckModsExtended.Models;
using CheckModsExtended.Services.UI;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Renders the user-facing console output for the mod-check workflow.
/// </summary>
public interface IModCheckReporter : ILoggingReporter, IResultRenderer, IUserPromptService { }
