namespace CheckModsExtended.Models;

/// <summary>
/// The action a user picks from the end-of-run menu.
/// </summary>
public enum EndOfRunChoice
{
    /// <summary>
    /// Open every mod page with an available update in the browser.
    /// </summary>
    OpenUpdatePages = 0,

    /// <summary>
    /// Open the ignore-management checklist for the available updates.
    /// </summary>
    ManageIgnoredUpdates = 1,

    /// <summary>
    /// Rescan the mods directory and check for updates again.
    /// </summary>
    Rescan = 3,

    /// <summary>
    /// Launch the Web GUI.
    /// </summary>
    LaunchWebGui = 4,

    /// <summary>
    /// Close the application.
    /// </summary>
    Exit = 2,
}
