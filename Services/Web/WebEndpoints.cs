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

    public static void MapEndpoints(WebApplication app, string[] args)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/status", () => Results.Ok(new StatusResponse("running", CheckModsExtended.Utils.VersionInfo.SemVer)));
        
        api.MapPost("/scan", async (CheckModsExtended.Services.Interfaces.IUpdateWorkflowOrchestrator orchestrator, CancellationToken token) => 
        {
            var mods = await orchestrator.RunPipelineAsync(args, token);
            
            var response = mods.Select(m => new ModDto(
                m.Api.ApiModId,
                m.DisplayName,
                m.DisplayAuthor,
                m.Local.LocalVersion,
                m.Update.LatestVersion ?? "Unknown",
                m.Update.UpdateStatus.ToString(),
                m.Local.IsServerMod,
                m.Api.ApiUrl,
                m.Update.DownloadLink
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
