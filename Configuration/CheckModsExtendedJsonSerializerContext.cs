using System.Collections.Generic;
using System.Text.Json.Serialization;
using CheckModsExtended.Models;
using CheckModsExtended.Utils;

namespace CheckModsExtended.Configuration;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(IgnoredUpdatesFile))]
[JsonSerializable(typeof(SptVersionApiResponse))]
[JsonSerializable(typeof(ModSearchResult))]
[JsonSerializable(typeof(CheckModsExtended.Models.Api.ModApiResponse))]
[JsonSerializable(typeof(ModSearchApiResponse))]
[JsonSerializable(typeof(ModByIdApiResponse))]
[JsonSerializable(typeof(ModUpdatesApiResponse))]
[JsonSerializable(typeof(ModDependenciesApiResponse))]
[JsonSerializable(typeof(List<IgnoreReportUrl.ReportEntry>))]
[JsonSerializable(typeof(List<Mod>))]
[JsonSerializable(typeof(IReadOnlyList<Mod>))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.StatusResponse))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.ScanResponse))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.ModDto))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.DependencyChangeDto))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.BlockingModDto))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.IgnoreRequest))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.MessageResponse))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.ErrorResponse))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.MisplacedModDto))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.CrossInstalledDirectoryDto))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.MisplacedModReportDto))]
[JsonSerializable(typeof(CheckModsExtended.Services.Web.ExportModDto))]
internal sealed partial class CheckModsExtendedJsonSerializerContext : JsonSerializerContext { }
