namespace CheckModsExtended.Models;

/// <summary>
/// Options for filtering and sorting list outputs.
/// </summary>
public record ListFilterOptions
{
    /// <summary>
    /// Filter by type (e.g., Server or Client).
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Filter by status or source.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Sort by a specific field.
    /// </summary>
    public string? Sort { get; init; }

    /// <summary>
    /// Limit the number of results returned.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Search by a text query.
    /// </summary>
    public string? Search { get; init; }
}
