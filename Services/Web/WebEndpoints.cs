using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CheckModsExtended.Services.Web;

/// <summary>
/// Registers the REST API endpoints for the Web Manager GUI.
/// </summary>
public static class WebEndpoints
{
    private static readonly string[] EmptyArgs = Array.Empty<string>();

    /// <summary>
    /// Maps the Web Manager API endpoints to the application.
    /// </summary>
    /// <param name="app">The web application instance.</param>
    /// <param name="args">Command line arguments passed to the application.</param>
    public static void MapEndpoints(WebApplication app, string[] args)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/status", () => Results.Ok(new StatusResponse("running", CheckModsExtended.Utils.VersionInfo.SemVer)));
        
        api.MapPost("/scan", async (CheckModsExtended.Services.Interfaces.IUpdateWorkflowOrchestrator orchestrator, CancellationToken token) => 
        {
            Spectre.Console.AnsiConsole.WriteLine($"[DEBUG] MapEndpoints args length: {args.Length}");
            if (args.Length > 0)
            {
                Spectre.Console.AnsiConsole.WriteLine($"[DEBUG] args[0]: {args[0]}");
            }
            var mods = await orchestrator.RunPipelineAsync(args, token);
            Spectre.Console.AnsiConsole.WriteLine($"[DEBUG] mods.Count: {mods.Count}");
            
            var response = mods.Select(m => new ModDto(
                Id: m.Api.ApiModId,
                Name: m.DisplayName,
                Author: m.DisplayAuthor,
                LocalVersion: m.Local.LocalVersion,
                LatestVersion: m.Update.LatestVersion ?? "Unknown",
                Status: m.Update.UpdateStatus.ToString(),
                IsServerMod: m.Local.IsServerMod,
                ModUrl: m.Api.ApiUrl,
                DownloadUrl: m.Update.DownloadLink,
                IncompatibilityReason: m.Update.IncompatibilityReason,
                CompatibleVersion: m.Update.CompatibleVersionString,
                BlockReason: m.Update.BlockReason,
                BlockingMods: m.Update.BlockingMods?.Select(b => new BlockingModDto(
                    ModId: b.ModId,
                    Name: b.Name,
                    Constraint: b.Constraint
                )).ToList(),
                AddedDependencies: m.Update.UpdateDependencyChanges?.Added.Select(d => new DependencyChangeDto(
                    ModId: d.ModId,
                    Slug: d.Slug ?? string.Empty,
                    Name: d.Name,
                    RecommendedVersion: d.RecommendedVersion,
                    InstalledVersion: d.InstalledVersion,
                    InstallState: d.InstallState.ToString(),
                    Conflict: d.Conflict,
                    DownloadLink: d.DownloadLink
                )).ToList(),
                RemovedDependencies: m.Update.UpdateDependencyChanges?.Removed.Select(d => new DependencyChangeDto(
                    ModId: d.ModId,
                    Slug: d.Slug ?? string.Empty,
                    Name: d.Name,
                    RecommendedVersion: d.RecommendedVersion,
                    InstalledVersion: d.InstalledVersion,
                    InstallState: d.InstallState.ToString(),
                    Conflict: d.Conflict,
                    DownloadLink: d.DownloadLink
                )).ToList()
            )).ToList();
            
            return Results.Ok(new ScanResponse(response));
        });
        
        api.MapPost("/ignore", async (CheckModsExtended.Services.Interfaces.IIgnoredUpdateStore ignoreStore, HttpRequest request, CancellationToken token) => 
        {
            var req = await request.ReadFromJsonAsync<IgnoreRequest>(cancellationToken: token);
            if (req != null && req.Id > 0 && !string.IsNullOrEmpty(req.LocalVersion) && !string.IsNullOrEmpty(req.LatestVersion))
            {
                var entry = new CheckModsExtended.Models.IgnoredUpdate(req.Id, req.LocalVersion, req.LatestVersion)
                {
                    Source = CheckModsExtended.Models.IgnoreSource.User,
                    DismissedUtc = DateTimeOffset.UtcNow
                };
                
                var existing = await ignoreStore.LoadAsync(token);
                var newList = existing.ToList();
                // Ensure we don't duplicate
                if (!newList.Any(x => x.Key == entry.Key))
                {
                    newList.Add(entry);
                    await ignoreStore.SaveAsync(newList, token);
                }
                
                return Results.Ok(new MessageResponse($"Ignored {req.Id}"));
            }
            return Results.BadRequest(new ErrorResponse("Missing or invalid mod parameters"));
        });
    }
}
