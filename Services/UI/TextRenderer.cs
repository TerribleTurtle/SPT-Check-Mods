using System;
using System.Collections.Generic;
using System.IO;
using CheckMods.Models;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.UI;

/// <summary>
/// Spectre.Console implementation of <see cref="ITextRenderer"/>.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TextRenderer : ITextRenderer
{
    private static readonly string[] _bannerTaglines =
    [
        "Cheeki breeki, your mods are peaky!",
        "No FiR tag required.",
        "Opachki! Your mods are showing.",
        "Warning: May cause gear fear.",
        "Fence would sell this for 3x the price.",
        "Not responsible for any leg meta incidents.",
        "Ref approved.",
        "Scav karma not affected by usage.",
        "No insurance fraud detected.",
        "Jaeger would make this a daily quest.",
        "Tested on scavs!",
        "More reliable than a PM pistol.",
        "Killa can't spawn here. You're safe.",
        "Side effects may include mod addiction.",
        "Lighthouse rogues hate this one simple trick!",
        "Your stash is safe. Your mods? Let's see...",
        "Better odds than finding a GPU in raid.",
        "Tagilla tested, Tagilla approved.",
        "No extract campers were consulted.",
        "Mechanic charges extra for this service.",
        "Labs keycard not required.",
        "Results may vary based on desync.",
        "Powered by strong coffee.",
        "Divide my cheeks!",
        "Won't fix your packet loss.",
        "Prapor's dogs won't find your lost mods.",
        "Extracting in 3... 2... 1...",
        "Awaits session start forever.",
        "Watch out for the Goons.",
        "Therapist wants to know your location.",
        "Head, Eyes.",
        "More terrifying than a cultist in the bushes.",
        "Dicky needles!",
    ];

    /// <inheritdoc />
    public void Banner()
    {
        var tagline = _bannerTaglines[Random.Shared.Next(_bannerTaglines.Length)];

        AnsiConsole.Write(new FigletText("Check Mods").LeftJustified().Color(Color.Blue));
        AnsiConsole.MarkupLine("[fuchsia]A tool to check for mod issues and updates.[/]");
        AnsiConsole.MarkupLine($"[grey]{tagline}[/]");
        AnsiConsole.MarkupLine("[link]https://forge.sp-tarkov.com[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc />
    public void Rule()
    {
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
    }

    /// <inheritdoc />
    public void Blank()
    {
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc />
    public void Heading(string text)
    {
        AnsiConsole.MarkupLine($"[bold blue]{text.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void Status(string text)
    {
        AnsiConsole.MarkupLine($"[grey]{text.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void Success(string text)
    {
        AnsiConsole.MarkupLine($"[green]{text.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void Warning(string text)
    {
        AnsiConsole.MarkupLine($"[yellow]{text.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void Error(string text)
    {
        AnsiConsole.MarkupLine($"[red]{text.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void Exception(Exception ex)
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    }

    /// <inheritdoc />
    public void CouldNotReadModDll(string fileName, string reason)
    {
        AnsiConsole.MarkupLine(
            $"[orange1]Warning:[/] Could not read mod DLL [grey]{fileName.EscapeMarkup()}[/]. Reason: {reason.EscapeMarkup()}"
        );
    }

    /// <inheritdoc />
    public void CouldNotReadSptVersion(string reason)
    {
        AnsiConsole.MarkupLine($"[orange1]Warning:[/] Could not read SPT version. Reason: {reason.EscapeMarkup()}");
    }

    /// <inheritdoc />
    public void PluginsDirectoryNotFound(string path)
    {
        AnsiConsole.MarkupLine(
            $"[orange1]Warning:[/] BepInEx plugins directory not found: [grey]{path.EscapeMarkup()}[/]"
        );
    }

    /// <inheritdoc />
    public void UsingPath(string path)
    {
        AnsiConsole.MarkupLine($"[grey]Using Path:[/] {path.EscapeMarkup()}");
    }

    /// <inheritdoc />
    public void DirectoryDoesNotExist(string path)
    {
        AnsiConsole.MarkupLine($"[red]Error: Directory does not exist: {path.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void ValidatingSptVersion(string version)
    {
        AnsiConsole.Markup(
            $"Found local SPT version [bold blue]{version.EscapeMarkup()}[/]. Validating with Forge API... "
        );
    }

    /// <inheritdoc />
    public void SptVersionValidated(string version)
    {
        AnsiConsole.MarkupLine($"[green]Successfully validated SPT Version:[/] [bold]{version.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void SptUpdateAvailable(SptVersionResult latest)
    {
        var versionDisplay = $"[bold]{latest.Version.EscapeMarkup()}[/]";

        if (latest.ModCount > 0)
        {
            versionDisplay += $" [grey]({latest.ModCount} mods)[/]";
        }

        AnsiConsole.MarkupLine($"[yellow]SPT update available:[/] {versionDisplay}");

        if (!string.IsNullOrWhiteSpace(latest.Link))
        {
            AnsiConsole.MarkupLine($"[grey]{latest.Link.EscapeMarkup()}[/]");
        }
    }

    /// <inheritdoc />
    public void CheckModsUpdate(CheckModsUpdateResult result, SemanticVersioning.Version sptVersion)
    {
        switch (result.Status)
        {
            case CheckModsUpdateStatus.UpdateAvailable:
                AnsiConsole.MarkupLine(
                    $"[yellow]A new version of Check Mods is available:[/] [bold]v{(result.LatestVersion ?? "?").EscapeMarkup()}[/] [grey](you have v{result.CurrentVersion.EscapeMarkup()})[/]"
                );
                if (!string.IsNullOrWhiteSpace(result.DownloadLink))
                {
                    AnsiConsole.MarkupLine($"[grey]Download:[/] [link]{result.DownloadLink.EscapeMarkup()}[/]");
                }
                break;

            case CheckModsUpdateStatus.UpToDate:
                AnsiConsole.MarkupLine(
                    $"[green]Check Mods is up to date (v{result.CurrentVersion.EscapeMarkup()}).[/]"
                );
                break;

            case CheckModsUpdateStatus.IncompatibleWithSpt:
                AnsiConsole.MarkupLine(
                    $"[grey]A newer version of Check Mods exists but isn't compatible with SPT {sptVersion.ToString().EscapeMarkup()}.[/]"
                );
                break;

            case CheckModsUpdateStatus.UnrecognizedBuild:
                AnsiConsole.MarkupLine(
                    $"[grey]You're running an unrecognized Check Mods build (v{result.CurrentVersion.EscapeMarkup()}). Consider the stable version on the Forge: v{(result.LatestVersion ?? "?").EscapeMarkup()}.[/]"
                );
                if (!string.IsNullOrWhiteSpace(result.DownloadLink))
                {
                    AnsiConsole.MarkupLine($"[grey]Download:[/] [link]{result.DownloadLink.EscapeMarkup()}[/]");
                }
                break;

            default:
                AnsiConsole.MarkupLine("[grey]Could not check for Check Mods updates.[/]");
                break;
        }

        AnsiConsole.WriteLine();
        Rule();
    }

    /// <inheritdoc />
    public void NoModsFound()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]No mods found.[/]");
        AnsiConsole.MarkupLine("[grey]Server mods should be located in:[/] SPT/user/mods");
        AnsiConsole.MarkupLine("[grey]Client mods should be located in:[/] BepInEx/plugins");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc />
    public bool PromptFetchRemoteIgnores()
    {
        return AnsiConsole.Prompt(
            new ConfirmationPrompt("Fetch the latest community ignore list from the Forge?") { DefaultValue = false }
        );
    }

    /// <inheritdoc />
    public void RemoteIgnoresMerged(int added)
    {
        AnsiConsole.MarkupLine(
            added > 0
                ? $"[green]Added {added} ignored version(s) from the community list.[/]"
                : "[grey]Your ignore list is already up to date.[/]"
        );
    }

    /// <inheritdoc />
    public void RemoteIgnoresUnavailable()
    {
        AnsiConsole.MarkupLine("[red]Couldn't fetch the community ignore list; your local entries are unchanged.[/]");
    }

    /// <inheritdoc />
    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates)
    {
        DrainBufferedKeys();
        AnsiConsole.WriteLine();

        var prompt = new SelectionPrompt<EndOfRunChoice>()
            .Title("[grey]What would you like to do?[/]")
            .HighlightStyle(Style.Parse("blue"))
            .UseConverter(choice => FormatEndOfRunChoice(choice, openableUpdateCount));

        if (openableUpdateCount > 0)
        {
            prompt.AddChoice(EndOfRunChoice.OpenUpdatePages);
        }

        if (canManageIgnoredUpdates)
        {
            prompt.AddChoice(EndOfRunChoice.ManageIgnoredUpdates);
        }

        prompt.AddChoice(EndOfRunChoice.Exit);

        return AnsiConsole.Prompt(prompt);
    }

    /// <inheritdoc />
    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds)
    {
        AnsiConsole.WriteLine();

        var prompt = new MultiSelectionPrompt<Mod>()
            .Title("Select the updates to [grey]ignore[/] (checked = treated as up to date):")
            .NotRequired()
            .PageSize(15)
            .MoreChoicesText("[grey](Move up and down to see more mods.)[/]")
            .InstructionsText("[grey](Space to toggle, enter to confirm. Checked entries are ignored.)[/]")
            .UseConverter(FormatIgnoreChoice);

        foreach (var mod in candidates)
        {
            var item = prompt.AddChoice(mod);
            if (mod.Api.ApiModId.HasValue && preIgnoredApiModIds.Contains(mod.Api.ApiModId.Value))
            {
                item.Select();
            }
        }

        return AnsiConsole.Prompt(prompt);
    }

    /// <inheritdoc />
    public void UpdatePagesOpened(int opened, int total)
    {
        if (total == 0)
        {
            return;
        }

        if (opened == total)
        {
            Success($"Opened {opened} mod page{Plural(opened)} in your browser.");
        }
        else if (opened == 0)
        {
            Error("Couldn't open your browser. The mod pages are listed as clickable links in the summary above.");
        }
        else
        {
            Warning(
                $"Opened {opened} of {total} mod pages; couldn't open the rest. The remaining pages are listed as clickable links above."
            );
        }
    }

    /// <inheritdoc />
    public bool PromptReportIgnores()
    {
        return AnsiConsole.Prompt(
            new ConfirmationPrompt("Report these ignored versions so other users benefit?") { DefaultValue = false }
        );
    }

    /// <inheritdoc />
    public void IgnoreReportOpened(string url, bool browserOpened, bool prefilled)
    {
        if (browserOpened)
        {
            AnsiConsole.MarkupLine("[green]Opening your browser to file the report. Thank you for contributing![/]");
        }
        else
        {
            AnsiConsole.MarkupLine(
                "[yellow]Couldn't open your browser automatically. Use this link to file the report:[/]"
            );
            AnsiConsole.MarkupLine($"[grey]{url.EscapeMarkup()}[/]");
        }

        if (!prefilled)
        {
            AnsiConsole.MarkupLine(
                "[grey]Your list was too large to pre-fill; paste the contents of your ignored-updates.json into the issue.[/]"
            );
        }
    }

    /// <inheritdoc />
    public void ApplicationFooter(string version, string hash, string logFilePath)
    {
        AnsiConsole.MarkupLine($"[grey]Check Mods v{version.EscapeMarkup()} (build {hash.EscapeMarkup()})[/]");
        AnsiConsole.MarkupLine($"[grey]Log file: {logFilePath.EscapeMarkup()}[/]");
    }

    private static string FormatEndOfRunChoice(EndOfRunChoice choice, int openableUpdateCount)
    {
        return choice switch
        {
            EndOfRunChoice.OpenUpdatePages =>
                $"Open {openableUpdateCount} mod page{Plural(openableUpdateCount)} with updates in your browser",
            EndOfRunChoice.ManageIgnoredUpdates => "Manage ignored updates",
            EndOfRunChoice.Exit => "Close Check Mods",
            _ => choice.ToString(),
        };
    }

    private static string Plural(int count)
    {
        return count == 1 ? string.Empty : "s";
    }

    private static string FormatIgnoreChoice(Mod mod)
    {
        var name = mod.DisplayName.EscapeMarkup();
        var local = mod.Local.LocalVersion.EscapeMarkup();
        var latest = (mod.Update.LatestVersion ?? "?").EscapeMarkup();
        return $"{name}  [grey]{local} -> {latest}[/]";
    }

    private static void DrainBufferedKeys()
    {
        if (Console.IsInputRedirected)
        {
            return;
        }

        while (Console.KeyAvailable)
        {
            Console.ReadKey(intercept: true);
        }
    }
}
