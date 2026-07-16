using System.IO;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services.UI;

/// <inheritdoc />
[Injectable(InjectionType.Singleton)]
public sealed class MisplacedModUiRenderer(ITextRenderer textRenderer) : IMisplacedModUiRenderer
{
    /// <inheritdoc />
    public void MisplacedMods(MisplacedModReport report)
    {
        AnsiConsole.WriteLine();
        textRenderer.Rule();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red bold]Improperly installed mods detected.[/]");
        AnsiConsole.MarkupLine(
            "[grey]It appears that the following mods are installed incorrectly. Review the mod pages for install instructions and ensure they are correctly installed.[/]"
        );
        AnsiConsole.WriteLine();

        var serverInClient = report.WrongFolder.Where(m => m.IsServerMod).ToList();
        var clientInServer = report.WrongFolder.Where(m => !m.IsServerMod).ToList();

        if (serverInClient.Count > 0)
        {
            var tree = new Tree(
                "[yellow]Server mods found in the client folder[/] [grey](BepInEx/plugins)[/][yellow]. Move them into[/] [grey]SPT/user/mods[/]"
            );
            foreach (var mod in serverInClient)
            {
                AddMisplacedModNode(tree, mod);
            }
            AnsiConsole.Write(tree);
            AnsiConsole.WriteLine();
        }

        if (clientInServer.Count > 0)
        {
            var tree = new Tree(
                "[yellow]Client mods found in the server folder[/] [grey](SPT/user/mods)[/][yellow]. Move them into[/] [grey]BepInEx/plugins[/]"
            );
            foreach (var mod in clientInServer)
            {
                AddMisplacedModNode(tree, mod);
            }
            AnsiConsole.Write(tree);
            AnsiConsole.WriteLine();
        }

        foreach (var directory in report.CrossInstalled)
        {
            PrintCrossInstalledDirectory(directory);
        }

        AnsiConsole.MarkupLine(
            "[red]These mods are being skipped for the rest of this check. Move them to the correct location and run this tool again to have them included.[/]"
        );
        AnsiConsole.MarkupLine("[grey]If this incorrect, please create a Github issue and provide logs.[/]");
        AnsiConsole.WriteLine();
        textRenderer.Rule();
    }

    private static void PrintCrossInstalledDirectory(CrossInstalledDirectory directory)
    {
        Tree tree;
        TreeNode directoryNode;

        if (directory.Ambiguous)
        {
            tree = new Tree(
                "[yellow]Unrelated mods share one folder under[/] [grey](BepInEx/plugins)[/][yellow]. One is likely in the wrong place. Review the install instructions for each[/]"
            );
            directoryNode = tree.AddNode($"[grey]{directory.Directory.EscapeMarkup()}[/]");
            foreach (var mod in directory.Mods)
            {
                AddMisplacedModNode(directoryNode, mod);
            }
        }
        else
        {
            tree = new Tree(
                "[yellow]Mods found inside another mod's folder under[/] [grey](BepInEx/plugins)[/][yellow]. Review the mod's installation instructions[/]"
            );
            directoryNode = tree.AddNode($"[grey]{directory.Directory.EscapeMarkup()}[/]");
            foreach (var mod in directory.Misplaced)
            {
                AddMisplacedModNode(directoryNode, mod);
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }

    private static void AddMisplacedModNode(IHasTreeNodes parent, MisplacedMod mod)
    {
        var name = !string.IsNullOrWhiteSpace(mod.Name) ? mod.Name : Path.GetFileName(mod.FilePath);
        var guidSuffix = !string.IsNullOrWhiteSpace(mod.Guid) ? $" [grey]({mod.Guid.EscapeMarkup()})[/]" : string.Empty;

        var modNode = parent.AddNode($"[white]{name.EscapeMarkup()}[/]{guidSuffix}");
        modNode.AddNode($"[grey]{mod.FilePath.EscapeMarkup()}[/]");
    }
}
