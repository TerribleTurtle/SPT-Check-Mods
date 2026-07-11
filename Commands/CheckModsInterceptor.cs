using CheckModsExtended.Configuration;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Intercepts commands before they execute to extract global flags into the <see cref="RuntimeConfig"/>.
/// </summary>
public sealed class CheckModsInterceptor : ICommandInterceptor
{
    private readonly RuntimeConfig _runtimeConfig;

    public CheckModsInterceptor(RuntimeConfig runtimeConfig)
    {
        _runtimeConfig = runtimeConfig;
    }

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is GlobalSettings globalSettings)
        {
            _runtimeConfig.IsHeadless = globalSettings.NoPrompt;
            _runtimeConfig.IsVerbose = globalSettings.Verbose;
        }
    }
}
