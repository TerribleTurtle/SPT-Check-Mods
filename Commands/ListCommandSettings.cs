using System.ComponentModel;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Settings for commands that list items.
/// </summary>
public class ListCommandSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the type filter.
    /// </summary>
    [CommandOption("-t|--type <TYPE>")]
    [Description("Filter by type (e.g., server, client)")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the status filter.
    /// </summary>
    [CommandOption("-s|--status <STATUS>")]
    [Description("Filter by status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the sort field.
    /// </summary>
    [CommandOption("--sort <SORT>")]
    [Description("Sort by field (e.g., name, author, version)")]
    public string? Sort { get; set; }

    /// <summary>
    /// Gets or sets the limit of results to show.
    /// </summary>
    [CommandOption("-l|--limit <LIMIT>")]
    [Description("Limit the number of results")]
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    [CommandOption("--search <SEARCH>")]
    [Description("Search by text")]
    public string? Search { get; set; }
}
