using System.Text.Json.Serialization;

namespace CheckModsExtended.Models.Api;

public sealed record ModApiResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] ModSearchResult? Data
);
