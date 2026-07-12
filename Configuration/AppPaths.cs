using System;
using System.IO;

namespace CheckModsExtended.Configuration;

/// <summary>
/// Configuration for the application paths.
/// </summary>
public sealed class AppPaths
{
    /// <summary>The directory holding Check Mods' app data (shared with logs and other local state).</summary>
    public string AppDataDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SptCheckModsExtended");
}
