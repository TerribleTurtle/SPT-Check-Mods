namespace CheckModsExtended.Configuration;

/// <summary>
/// Configuration options for the mod scanner service.
/// </summary>
public sealed class ModScannerOptions
{
    /// <summary>
    /// Maximum DLL file size in bytes to scan (default: 100MB).
    /// Prevents OutOfMemoryExceptions or excessive CPU consumption when
    /// attempting to parse BepInPlugin attributes from extraordinarily large binaries.
    /// </summary>
    public long MaxDllSizeBytes { get; set; } = 100 * 1024 * 1024;
}
