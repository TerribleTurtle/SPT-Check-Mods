using System;
using CheckModsExtended.Services.Web;

namespace CheckModsExtended.Models;

/// <summary>
/// Represents a cached mod scan result for fast cold starts.
/// </summary>
public record ScanCacheRecord(
    DateTimeOffset CachedAtUtc,
    string? SptPath,
    ScanResponse Response
);
