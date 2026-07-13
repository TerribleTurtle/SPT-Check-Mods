using CheckModsExtended.Configuration;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Intercepts commands before they execute to extract global flags into the <see cref="RuntimeConfig"/>.
/// </summary>
public sealed class CheckModsInterceptor : ICommandInterceptor
{
    private readonly RuntimeConfig _runtimeConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckModsInterceptor"/> class.
    /// </summary>
    /// <param name="runtimeConfig">The runtime configuration.</param>
    public CheckModsInterceptor(RuntimeConfig runtimeConfig)
    {
        _runtimeConfig = runtimeConfig;
    }

    /// <summary>
    /// Intercepts the command execution to apply global settings.
    /// This includes setting up the implicit headless mode side-effect: 
    /// when outputting machine-readable formats (e.g., json, markdown), 
    /// headless mode is implicitly enforced to prevent interactive prompts 
    /// from polluting the structured output stream.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The parsed command settings.</param>
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is GlobalSettings globalSettings)
        {
            _runtimeConfig.IsHeadless = globalSettings.NoPrompt;
            _runtimeConfig.IsVerbose = globalSettings.Verbose;
            _runtimeConfig.Format = globalSettings.Format;

            // Implicit headless mode side-effect:
            // When outputting machine-readable formats (e.g., json, markdown), we implicitly
            // enforce headless mode to prevent interactive prompts from polluting the structured output stream.
            if (!_runtimeConfig.Format.Equals("table", System.StringComparison.OrdinalIgnoreCase))
            {
                _runtimeConfig.IsHeadless = true;
            }
        }
    }
}
