namespace CheckModsExtended.Services.Web;

/// <summary>
/// Response returned from the status API endpoint.
/// </summary>
public record StatusResponse(
    string Status,
    string Version,
    string? SptVersion,
    string? LatestAppVersion,
    bool AppUpdateAvailable
);

/// <summary>
/// Response returned from the scan API endpoint.
/// </summary>
public record ScanResponse(
    List<ModDto> Mods,
    MisplacedModReportDto? MisplacedMods,
    string? SptVersion
);

/// <summary>
/// Represents a required or removed dependency for a mod update.
/// </summary>
public record DependencyChangeDto(int ModId, string Slug, string Name, string RecommendedVersion, string? InstalledVersion, string InstallState, bool Conflict, string? DownloadLink);

/// <summary>
/// Represents a mod that is blocking an update.
/// </summary>
public record BlockingModDto(int ModId, string Name, string Constraint);

/// <summary>
/// Data transfer object representing a mod and its update status.
/// </summary>
public record ModDto(
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
    IReadOnlyList<string>? LoadWarnings = null,
    bool IsIgnored = false,
    bool IsPaired = false,
    string? LocalDirectory = null
);

public record MisplacedModDto(string Name, string Version, string FilePath, bool IsServerMod);

public record CrossInstalledDirectoryDto(
    string Directory,
    IReadOnlyList<MisplacedModDto> Mods,
    bool Ambiguous
);

public record MisplacedModReportDto(
    IReadOnlyList<MisplacedModDto> WrongFolder,
    IReadOnlyList<CrossInstalledDirectoryDto> CrossInstalled
);

public record ExportModDto(
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
public record IgnoreRequest(int Id, string LocalVersion, string LatestVersion);

/// <summary>
/// Generic message response.
/// </summary>
public record MessageResponse(string Message);

/// <summary>
/// Generic error response.
/// </summary>
public record ErrorResponse(string Error);

public record OpenSystemRequest(string Target);
