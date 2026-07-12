namespace CheckModsExtended.Services.Web;

public record StatusResponse(string Status, string Version);
public record ScanResponse(List<ModDto> Mods);
public record ModDto(int? Id, string Name, string Author, string LocalVersion, string LatestVersion, string Status, bool IsServerMod, string? ModUrl, string? DownloadUrl);
public record IgnoreRequest(int Id, string LocalVersion, string LatestVersion);
public record MessageResponse(string Message);
public record ErrorResponse(string Error);
