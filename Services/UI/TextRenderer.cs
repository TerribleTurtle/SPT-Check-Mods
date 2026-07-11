using System;
using System.Collections.Generic;
using System.IO;
using CheckModsExtended.Models;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services.UI;

/// <summary>
/// Spectre.Console implementation of <see cref="ITextRenderer"/>.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TextRenderer : ITextRenderer
{
    public void Banner()
    {
        var tagline = BannerTaglineProvider.GetRandomTagline();

        AnsiConsole.Write(new FigletText("Check Mods").LeftJustified().Color(Color.Blue));
        AnsiConsole.MarkupLine("[fuchsia]A tool to check for mod issues and updates.[/]");
        AnsiConsole.MarkupLine($"[grey]{tagline}[/]");
        AnsiConsole.MarkupLine("[link]https://forge.sp-tarkov.com[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
        AnsiConsole.WriteLine();
    }

    public void Rule()
    {
        AnsiConsole.Write(new Rule().RuleStyle("grey"));
    }

    public void Blank()
    {
        AnsiConsole.WriteLine();
    }

    public void Heading(string text)
    {
        AnsiConsole.MarkupLine($"[bold blue]{text.EscapeMarkup()}[/]");
    }

    public void Status(string text)
    {
        AnsiConsole.MarkupLine($"[grey]{text.EscapeMarkup()}[/]");
    }

    public void Success(string text)
    {
        AnsiConsole.MarkupLine($"[green]{text.EscapeMarkup()}[/]");
    }

    public void Warning(string text)
    {
        AnsiConsole.MarkupLine($"[yellow]{text.EscapeMarkup()}[/]");
    }

    public void Error(string text)
    {
        AnsiConsole.MarkupLine($"[red]{text.EscapeMarkup()}[/]");
    }

    public void Exception(Exception ex)
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    }

    public void CouldNotReadModDll(string fileName, string reason)
    {
        AnsiConsole.MarkupLine(
            $"[orange1]Warning:[/] Could not read mod DLL [grey]{fileName.EscapeMarkup()}[/]. Reason: {reason.EscapeMarkup()}"
        );
    }

    public void CouldNotReadSptVersion(string reason)
    {
        AnsiConsole.MarkupLine($"[orange1]Warning:[/] Could not read SPT version. Reason: {reason.EscapeMarkup()}");
    }

    public void PluginsDirectoryNotFound(string path)
    {
        AnsiConsole.MarkupLine(
            $"[orange1]Warning:[/] BepInEx plugins directory not found: [grey]{path.EscapeMarkup()}[/]"
        );
    }

    public void UsingPath(string path)
    {
        AnsiConsole.MarkupLine($"[grey]Using Path:[/] {path.EscapeMarkup()}");
    }

    public void DirectoryDoesNotExist(string path)
    {
        AnsiConsole.MarkupLine($"[red]Error: Directory does not exist: {path.EscapeMarkup()}[/]");
    }

    public void ValidatingSptVersion(string version)
    {
        AnsiConsole.Markup(
            $"Found local SPT version [bold blue]{version.EscapeMarkup()}[/]. Validating with Forge API... "
        );
    }

    public void SptVersionValidated(string version)
    {
        AnsiConsole.MarkupLine($"[green]Successfully validated SPT Version:[/] [bold]{version.EscapeMarkup()}[/]");
    }

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

    public void CheckModsExtendedUpdate(CheckModsExtendedUpdateResult result, SemanticVersioning.Version sptVersion)
    {
        switch (result.Status)
        {
            case CheckModsExtendedUpdateStatus.UpdateAvailable:
                AnsiConsole.MarkupLine(
                    $"[yellow]A new version of Check Mods is available:[/] [bold]v{(result.LatestVersion ?? "?").EscapeMarkup()}[/] [grey](you have v{result.CurrentVersion.EscapeMarkup()})[/]"
                );
                if (!string.IsNullOrWhiteSpace(result.DownloadLink))
                {
                    AnsiConsole.MarkupLine($"[grey]Download:[/] [link]{result.DownloadLink.EscapeMarkup()}[/]");
                }
                break;

            case CheckModsExtendedUpdateStatus.UpToDate:
                AnsiConsole.MarkupLine(
                    $"[green]Check Mods is up to date (v{result.CurrentVersion.EscapeMarkup()}).[/]"
                );
                break;

            case CheckModsExtendedUpdateStatus.IncompatibleWithSpt:
                AnsiConsole.MarkupLine(
                    $"[grey]A newer version of Check Mods exists but isn't compatible with SPT {sptVersion.ToString().EscapeMarkup()}.[/]"
                );
                break;

            case CheckModsExtendedUpdateStatus.UnrecognizedBuild:
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

    public void NoModsFound()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]No mods found.[/]");
        AnsiConsole.MarkupLine("[grey]Server mods should be located in:[/] SPT/user/mods");
        AnsiConsole.MarkupLine("[grey]Client mods should be located in:[/] BepInEx/plugins");
        AnsiConsole.WriteLine();
    }

    public void RemoteIgnoresMerged(int added)
    {
        AnsiConsole.MarkupLine(
            added > 0
                ? $"[green]Added {added} ignored version(s) from the community list.[/]"
                : "[grey]Your ignore list is already up to date.[/]"
        );
    }

    public void RemoteIgnoresUnavailable()
    {
        AnsiConsole.MarkupLine("[red]Couldn't fetch the community ignore list; your local entries are unchanged.[/]");
    }

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

    public void ApplicationFooter(string version, string hash, string logFilePath)
    {
        AnsiConsole.MarkupLine($"[grey]Check Mods v{version.EscapeMarkup()} (build {hash.EscapeMarkup()})[/]");
        AnsiConsole.MarkupLine($"[grey]Log file: {logFilePath.EscapeMarkup()}[/]");
    }

    private static string Plural(int count)
    {
        return count == 1 ? string.Empty : "s";
    }

    public void IgnoreAddAlreadyIgnored(int apiModId, string localVersion, string latestVersion)
    {
        AnsiConsole.MarkupLine(
            $"[yellow]Update is already ignored (ID: {apiModId}, {localVersion.EscapeMarkup()} -> {latestVersion.EscapeMarkup()}).[/]"
        );
    }

    public void IgnoreAddSuccess(int apiModId, string localVersion, string latestVersion)
    {
        AnsiConsole.MarkupLine(
            $"[green]Successfully ignored update for API Mod ID {apiModId} ({localVersion.EscapeMarkup()} -> {latestVersion.EscapeMarkup()}).[/]"
        );
    }

    public void IgnoreRemoveNotFound(int apiModId)
    {
        AnsiConsole.MarkupLine($"[yellow]No ignored updates found for API Mod ID {apiModId}.[/]");
    }

    public void IgnoreRemoveSuccess(int removedCount, int apiModId)
    {
        AnsiConsole.MarkupLine(
            $"[green]Successfully removed {removedCount} ignored update(s) for API Mod ID {apiModId}.[/]"
        );
    }
}
