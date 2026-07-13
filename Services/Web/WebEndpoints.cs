using System;
using System.Linq;
using System.Threading;
using CheckModsExtended.Models.Pipeline;
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
    private static readonly SemaphoreSlim _statusLock = new(1, 1);

    public static void MapEndpoints(WebApplication app, string[] args)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/status", async (
            CheckModsExtended.Services.Interfaces.ISptInstallationService sptInstall,
            CheckModsExtended.Services.Interfaces.IUpdateCheckService updateCheck,
            CancellationToken token) =>
        {
            await _statusLock.WaitAsync(token);
            try
            {
                var path = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
                var sptVer = await sptInstall.GetAndValidateSptVersionAsync(path, token);

                bool updateAvailable = false;
                string? latestAppVersion = null;
                if (sptVer != null)
                {
                    var updateInfo = await updateCheck.CheckAsync(sptVer, token);
                    updateAvailable = updateInfo.Status == CheckModsExtended.Models.CheckModsExtendedUpdateStatus.UpdateAvailable;
                    latestAppVersion = updateInfo.LatestVersion;
                }

                return Results.Ok(new StatusResponse("running", CheckModsExtended.Utils.VersionInfo.SemVer, sptVer?.ToString(), latestAppVersion, updateAvailable));
            }
            finally
            {
                _statusLock.Release();
            }
        });

        api.MapGet("/cache", async (CheckModsExtended.Services.Interfaces.IScanCacheService cacheService, CancellationToken token) =>
        {
            var cache = await cacheService.LoadCacheAsync(token);
            if (cache != null)
            {
                return Results.Ok(cache);
            }
            return Results.NotFound(new ErrorResponse("No cache available"));
        });

        api.MapPost("/scan", async (CheckModsExtended.Services.Interfaces.IUpdateWorkflowOrchestrator orchestrator, CancellationToken token) =>
        {
            var context = await orchestrator.RunPipelineAsync(args, token);
            var response = ScanResponseMapper.Map(context);
            return Results.Ok(response);
        });

        api.MapPost("/ignore", async (CheckModsExtended.Services.Interfaces.IIgnoredUpdateStore ignoreStore, HttpRequest request, CancellationToken token) =>
        {
            var req = await request.ReadFromJsonAsync(CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, cancellationToken: token) as IgnoreRequest;
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

        api.MapGet("/ignores", async (CheckModsExtended.Services.Interfaces.IIgnoredUpdateStore ignoreStore, CancellationToken token) =>
        {
            var existing = await ignoreStore.LoadAsync(token);
            return Results.Ok(existing);
        });

        api.MapDelete("/ignore/{modId}", async (int modId, CheckModsExtended.Services.Interfaces.IIgnoredUpdateStore ignoreStore, CancellationToken token) =>
        {
            var existing = await ignoreStore.LoadAsync(token);
            var newList = existing.Where(x => x.ApiModId != modId).ToList();
            await ignoreStore.SaveAsync(newList, token);
            return Results.Ok(new MessageResponse($"Removed ignore for {modId}"));
        });

        app.MapPost("/api/system/open", (OpenSystemRequest req, CheckModsExtended.Services.Interfaces.IBrowserLauncher browserLauncher) =>
        {
            if (string.IsNullOrWhiteSpace(req.Target))
            {
                return Results.BadRequest(new ErrorResponse("Target is required"));
            }

            var result = browserLauncher.TryOpenUrl(req.Target);
            return result.Match(
                success => Results.Ok(new MessageResponse("Opened target")),
                apiError => Results.BadRequest(new ErrorResponse(apiError.Message))
            );
        });
    }
}
