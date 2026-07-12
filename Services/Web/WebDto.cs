namespace CheckModsExtended.Services.Web;

/// <summary>
/// Response returned from the status API endpoint.
/// </summary>
public record StatusResponse(string Status, string Version);

/// <summary>
/// Response returned from the scan API endpoint.
/// </summary>
public record ScanResponse(List<ModDto> Mods);

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
    List<DependencyChangeDto>? RemovedDependencies = null
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
