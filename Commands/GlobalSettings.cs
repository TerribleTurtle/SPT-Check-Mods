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
    [Description("Enable verbose/debug logging output.")]
    public bool Verbose { get; set; }
}
