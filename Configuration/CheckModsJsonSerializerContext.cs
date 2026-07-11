using System.Collections.Generic;
using System.Text.Json.Serialization;
using CheckMods.Models;
using CheckMods.Utils;

namespace CheckMods.Configuration;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(IgnoredUpdatesFile))]
[JsonSerializable(typeof(SptVersionApiResponse))]
[JsonSerializable(typeof(ModSearchResult))]
[JsonSerializable(typeof(ModSearchApiResponse))]
[JsonSerializable(typeof(ModUpdatesApiResponse))]
[JsonSerializable(typeof(ModDependenciesApiResponse))]
[JsonSerializable(typeof(List<IgnoreReportUrl.ReportEntry>))]
internal partial class CheckModsJsonSerializerContext : JsonSerializerContext
{
}
