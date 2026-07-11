using System.Collections.Generic;
using System.Text.Json.Serialization;
using CheckModsExtended.Models;
using CheckModsExtended.Utils;

namespace CheckModsExtended.Configuration;

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
internal sealed partial class CheckModsExtendedJsonSerializerContext : JsonSerializerContext
{
}

