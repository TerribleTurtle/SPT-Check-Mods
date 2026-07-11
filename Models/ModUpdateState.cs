namespace CheckMods.Models;

public sealed class ModUpdateState
{
    public string? LatestVersion { get; set; }
    public UpdateStatus UpdateStatus { get; set; } = UpdateStatus.Unknown;
    public string? DownloadLink { get; set; }
    public IReadOnlyList<BlockingModInfo>? BlockingMods { get; set; }
    public string? BlockReason { get; set; }
    public string? IncompatibilityReason { get; set; }
    public bool IsLocalSptIncompatible { get; set; }
    public string? CompatibleVersionString { get; set; }
    public bool UpdateSuppressed { get; set; }
    public UpdateDependencyDelta? UpdateDependencyChanges { get; set; }
}
