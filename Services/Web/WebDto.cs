namespace CheckModsExtended.Services.Web;

/// <summary>
/// Response returned from the status API endpoint.
/// </summary>
public sealed record StatusResponse(
    string Status,
    string Version,
    string? SptVersion,
    string? LatestAppVersion,
    bool AppUpdateAvailable
);

/// <summary>
/// Response returned from the scan API endpoint.
/// </summary>
public sealed record ScanResponse(
    List<ModDto> Mods,
    MisplacedModReportDto? MisplacedMods,
    string? SptVersion
);

/// <summary>
/// Represents a required or removed dependency for a mod update.
/// </summary>
public sealed record DependencyChangeDto(int ModId, string Slug, string Name, string RecommendedVersion, string? InstalledVersion, string InstallState, bool Conflict, string? DownloadLink);

/// <summary>
/// Represents a mod that is blocking an update.
/// </summary>
public sealed record BlockingModDto(int ModId, string Name, string Constraint);

/// <summary>
/// Data transfer object representing a mod and its update status.
/// </summary>
public sealed record ModDto(
    int? Id,
    string Name,
    string Author,
    string LocalVersion,
    string LatestVersion,
    string Status,
    bool IsServerMod,
    string? ModUrl,
    string? DownloadUrl,
    string? IncompatibilityReason = null,
    string? CompatibleVersion = null,
    string? BlockReason = null,
    List<BlockingModDto>? BlockingMods = null,
    List<DependencyChangeDto>? AddedDependencies = null,
    List<DependencyChangeDto>? RemovedDependencies = null,
    string? SourceCodeUrl = null,
    string? LocalSptVersion = null,
    bool HasWarnings = false,
    bool IsDuplicate = false,
    IReadOnlyList<string>? LoadWarnings = null,
    bool IsIgnored = false,
    bool IsPaired = false,
    string? LocalDirectory = null,
    string? IgnoreSource = null
);

public sealed record MisplacedModDto(string Name, string Version, string FilePath, bool IsServerMod);

public sealed record CrossInstalledDirectoryDto(
    string Directory,
    IReadOnlyList<MisplacedModDto> Mods,
    bool Ambiguous
);

public sealed record MisplacedModReportDto(
    IReadOnlyList<MisplacedModDto> WrongFolder,
    IReadOnlyList<CrossInstalledDirectoryDto> CrossInstalled
);

public sealed record ExportModDto(
    string Name,
    string Author,
    string LocalVersion,
    string? LatestVersion,
    string Status,
    string Type,
    bool IsPaired,
    IReadOnlyList<string>? Dependencies
);

/// <summary>
/// Request payload for ignoring a mod update.
/// </summary>
public sealed record IgnoreRequest(int Id, string LocalVersion, string LatestVersion);

/// <summary>
/// Generic message response.
/// </summary>
public sealed record MessageResponse(string Message);

/// <summary>
/// Generic error response.
/// </summary>
public sealed record ErrorResponse(string Error);

public sealed record OpenSystemRequest(string Target);
