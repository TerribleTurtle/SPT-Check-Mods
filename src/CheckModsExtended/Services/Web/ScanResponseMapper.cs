using System;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;

namespace CheckModsExtended.Services.Web;

/// <summary>
/// Maps the update workflow context to the web API scan response DTO.
/// </summary>
public static class ScanResponseMapper
{
    public static ScanResponse Map(UpdateWorkflowContext context)
    {
        var response = context.Mods.Select(m => m.ToDto()).ToList();

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

    public static MisplacedModDto ToDto(this MisplacedMod m)
    {
        return new MisplacedModDto(
            Name: m.Name,
            Version: m.Version,
            FilePath: m.FilePath,
            IsServerMod: m.IsServerMod
        );
    }

    public static ModDto ToDto(this Mod m)
    {
        return new ModDto(
            Id: m.Api.ApiModId,
            Name: m.DisplayName,
            LocalName: m.Local.LocalName,
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
        );
    }

    public static Mod ToDomain(this ModDto dto)
    {
        return new Mod
        {
            Local = new LocalModIdentity
            {
                Guid = dto.Id?.ToString() ?? Guid.NewGuid().ToString(),
                LocalName = dto.LocalName ?? dto.Name,
                LocalAuthor = dto.Author,
                LocalVersion = dto.LocalVersion,
                IsServerMod = dto.IsServerMod,
                FilePath = dto.LocalDirectory != null ? System.IO.Path.Combine(dto.LocalDirectory, "package.json") : "",
                LocalSptVersion = dto.LocalSptVersion,
                PairedComponentPath = dto.IsPaired ? "paired" : null
            },
            Api = new ForgeApiMetadata
            {
                ApiModId = dto.Id,
                ApiName = dto.Name,
                ApiAuthor = new CheckModsExtended.Models.ModAuthor(0, dto.Author, null),
                ApiUrl = dto.ModUrl,
                ApiSourceCodeUrl = dto.SourceCodeUrl
            },
            Update = new ModUpdateState
            {
                UpdateStatus = Enum.TryParse<UpdateStatus>(dto.Status, out var status) ? status : UpdateStatus.Unknown,
                LatestVersion = dto.LatestVersion == "Unknown" ? null : dto.LatestVersion,
                DownloadLink = dto.DownloadUrl,
                IncompatibilityReason = dto.IncompatibilityReason,
                CompatibleVersionString = dto.CompatibleVersion,
                BlockReason = dto.BlockReason,
                UpdateSuppressed = dto.IsIgnored,
                UpdateSuppressedSource = Enum.TryParse<CheckModsExtended.Models.IgnoreSource>(dto.IgnoreSource, out var igSource) ? igSource : null
            },
            Status = Enum.TryParse<UpdateStatus>(dto.Status, out _) ? ModStatus.Verified : ModStatus.NoMatch,
            IsDuplicate = dto.IsDuplicate,
            LoadWarnings = dto.LoadWarnings ?? []
        };
    }
}
