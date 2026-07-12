using System.ComponentModel;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Global command settings shared across all commands.
/// </summary>
public class GlobalSettings : CommandSettings
{
    [CommandOption("-y|--no-prompt")]
    [Description("Run in headless mode and skip interactive prompts. Also infers all defaults.")]
    public bool NoPrompt { get; set; }

    [CommandOption("-v|--verbose")]
    [Description("Enable verbose logging output.")]
    public bool Verbose { get; set; }

    [CommandOption("-d|--debug")]
    [Description("Enable debug logging output (includes stack traces).")]
    public bool Debug { get; set; }

    [CommandOption("-f|--format <TYPE>")]
    [Description("Sets the output format. Valid values: table, json, csv. Defaults to table.")]
    [DefaultValue("table")]
    public string Format { get; set; } = "table";
}
