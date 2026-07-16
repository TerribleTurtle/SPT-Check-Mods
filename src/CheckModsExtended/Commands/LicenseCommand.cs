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
    /// <summary>
    /// Settings for the license command.
    /// </summary>
    public sealed class Settings : GlobalSettings { }

    /// <summary>
    /// Executes the license command asynchronously.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous execution operation. The task result contains the exit code.</returns>
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        return ExecuteInternalAsync(context, settings, cancellationToken);
    }

    /// <summary>
    /// Internal execution logic for the license command.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous execution operation. The task result contains the exit code.</returns>
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
