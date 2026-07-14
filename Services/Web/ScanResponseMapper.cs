using System.Linq;
using CheckModsExtended.Models.Pipeline;

namespace CheckModsExtended.Services.Web;

/// <summary>
/// Maps the update workflow context to the web API scan response DTO.
/// </summary>
public static class ScanResponseMapper
{
    public static ScanResponse Map(UpdateWorkflowContext context)
    {
        var response = context.Mods.Select(m => new ModDto(
            Id: m.Api.ApiModId,
            Name: m.DisplayName,
            Author: m.DisplayAuthor,
            LocalVersion: m.Local.LocalVersion,
            LatestVersion: m.Update.LatestVersion ?? "Unknown",
            Status: m.Update.UpdateStatus.ToString(),
            IsServerMod: m.Local.IsServerMod,
            ModUrl: m.Api.ApiUrl,
            DownloadUrl: m.Update.DownloadLink,
            IncompatibilityReason: m.Update.IncompatibilityReason,
            CompatibleVersion: m.Update.CompatibleVersionString,
            BlockReason: m.Update.BlockReason,
            BlockingMods: m.Update.BlockingMods?.Select(b => new BlockingModDto(
                ModId: b.ModId,
                Name: b.Name,
                Constraint: b.Constraint
            )).ToList(),
            AddedDependencies: m.Update.UpdateDependencyChanges?.Added.Select(d => new DependencyChangeDto(
                ModId: d.ModId,
                Slug: d.Slug ?? string.Empty,
                Name: d.Name,
                RecommendedVersion: d.RecommendedVersion,
                InstalledVersion: d.InstalledVersion,
                InstallState: d.InstallState.ToString(),
                Conflict: d.Conflict,
                DownloadLink: d.DownloadLink
            )).ToList(),
            RemovedDependencies: m.Update.UpdateDependencyChanges?.Removed.Select(d => new DependencyChangeDto(
                ModId: d.ModId,
                Slug: d.Slug ?? string.Empty,
                Name: d.Name,
                RecommendedVersion: d.RecommendedVersion,
                InstalledVersion: d.InstalledVersion,
                InstallState: d.InstallState.ToString(),
                Conflict: d.Conflict,
                DownloadLink: d.DownloadLink
            )).ToList(),
            SourceCodeUrl: m.Api.ApiSourceCodeUrl,
            LocalSptVersion: m.Local.LocalSptVersion,
            HasWarnings: m.HasWarnings,
            IsDuplicate: m.IsDuplicate,
            LoadWarnings: m.LoadWarnings.Count > 0 ? m.LoadWarnings.ToList() : null,
            IsIgnored: m.Update.UpdateSuppressed,
            IsPaired: m.Local.PairedComponentPath != null,
            LocalDirectory: m.Local.FilePath != null ? System.IO.Path.GetDirectoryName(m.Local.FilePath) : null,
            IgnoreSource: m.Update.UpdateSuppressedSource?.ToString()
        )).ToList();

        MisplacedModReportDto? misplacedReportDto = null;
        if (context.MisplacedReport != null && context.MisplacedReport.Any)
        {
            misplacedReportDto = new MisplacedModReportDto(
                WrongFolder: context.MisplacedReport.WrongFolder.Select(m => new MisplacedModDto(
                    Name: m.Name,
                    Version: m.Version,
                    FilePath: m.FilePath,
                    IsServerMod: m.IsServerMod
                )).ToList(),
                CrossInstalled: context.MisplacedReport.CrossInstalled.Select(d => new CrossInstalledDirectoryDto(
                    Directory: d.Directory,
                    Mods: d.Mods.Select(m => new MisplacedModDto(
                        Name: m.Name,
                        Version: m.Version,
                        FilePath: m.FilePath,
                        IsServerMod: m.IsServerMod
                    )).ToList(),
                    Ambiguous: d.Ambiguous
                )).ToList()
            );
        }

        return new ScanResponse(response, misplacedReportDto, context.SptVersion?.ToString());
    }
}
