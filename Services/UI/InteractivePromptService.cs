using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services.UI;

[Injectable(InjectionType.Singleton)]
public sealed class InteractivePromptService : IUserPromptService
{
    private readonly RuntimeConfig _runtimeConfig;

    public InteractivePromptService(RuntimeConfig runtimeConfig)
    {
        _runtimeConfig = runtimeConfig;
    }

    private bool IsHeadless()
    {
        return Console.IsInputRedirected || _runtimeConfig.IsHeadless;
    }

    public bool PromptFetchRemoteIgnores()
    {
        if (IsHeadless())
        {
            return false;
        }

        return AnsiConsole.Prompt(new ConfirmationPrompt("Fetch the latest community ignore list from the Forge?") { DefaultValue = false });
    }

    public EndOfRunChoice PromptEndOfRun(int openableUpdateCount, bool canManageIgnoredUpdates)
    {
        if (IsHeadless())
        {
            return EndOfRunChoice.Exit;
        }

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

    public IReadOnlyList<Mod> SelectUpdatesToIgnore(IReadOnlyList<Mod> candidates, ISet<int> preIgnoredApiModIds)
    {
        if (IsHeadless())
        {
            var results = new List<Mod>();
            foreach (var mod in candidates)
            {
                if (mod.Api.ApiModId.HasValue && preIgnoredApiModIds.Contains(mod.Api.ApiModId.Value))
                {
                    results.Add(mod);
                }
            }

            return results;
        }

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

    public bool PromptReportIgnores()
    {
        if (IsHeadless())
        {
            return false;
        }

        return AnsiConsole.Prompt(new ConfirmationPrompt("Report these ignored versions so other users benefit?") { DefaultValue = false });
    }

    public async Task<bool> PromptForConfirmationAsync(PendingConfirmation confirmation)
    {
        var displayMod = confirmation.OriginalMod.Local;
        return await AnsiConsole.ConfirmAsync(
            $"[yellow]Is '[white]{displayMod.LocalName.EscapeMarkup()}[/]' by '[white]{(displayMod.LocalAuthor ?? "Unknown").EscapeMarkup()}[/]' the same as '[white]{confirmation.ApiMatch.Name.EscapeMarkup()}[/]' by '[white]{(confirmation.ApiMatch.Owner?.Name ?? "N/A").EscapeMarkup()}[/]'? ([grey]Confidence: {confirmation.ConfidenceScore}%[/])[/]"
        );
    }

    private static string FormatEndOfRunChoice(EndOfRunChoice choice, int openableUpdateCount)
    {
        return choice switch
        {
            EndOfRunChoice.OpenUpdatePages => $"Open {openableUpdateCount} mod page{(openableUpdateCount == 1 ? "" : "s")} with updates in your browser",
            EndOfRunChoice.ManageIgnoredUpdates => "Manage ignored updates",
            EndOfRunChoice.Exit => "Close Check Mods",
            _ => choice.ToString(),
        };
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
