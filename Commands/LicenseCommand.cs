using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to display the LICENSE file.
/// </summary>
public sealed class LicenseCommand : AsyncCommand<LicenseCommand.Settings>
{
    public sealed class Settings : GlobalSettings { }

    protected override Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        return ExecuteInternalAsync(context, settings, cancellationToken);
    }

    internal static Task<int> ExecuteInternalAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "CheckModsExtended.LICENSE";

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            AnsiConsole.MarkupLine("[red]Error: LICENSE file not found in assembly.[/]");
            return Task.FromResult(1);
        }

        using StreamReader reader = new StreamReader(stream);
        var licenseText = reader.ReadToEnd();
        AnsiConsole.WriteLine(licenseText);

        return Task.FromResult(0);
    }
}
