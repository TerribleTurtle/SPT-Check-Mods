using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CheckModsExtended.Services.Web;

/// <summary>
/// Registers the REST API endpoints for the Web Manager GUI.
/// </summary>
public static class WebEndpoints
{
    private static readonly SemaphoreSlim _statusLock = new(1, 1);
    private static string[] _args = Array.Empty<string>();

    /// <summary>
    /// Maps the Web Manager endpoints to the provided application.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="args">Command line arguments.</param>
    public static void MapEndpoints(WebApplication app, string[] args)
    {
        _args = args;
        RouteGroupBuilder api = app.MapGroup("/api");

        api.MapGet("/status", GetStatusAsync);
        api.MapGet("/cache", GetCacheAsync);
        api.MapPost("/scan", PostScanAsync);
        api.MapPost("/ignore", PostIgnoreAsync);
        api.MapGet("/ignores", GetIgnoresAsync);
        api.MapDelete("/ignore/{modId}", DeleteIgnoreAsync);
        SettingsEndpoints.MapSettingsEndpoints(api);
        app.MapPost("/api/system/open", PostSystemOpen);
    }

    private static async Task<IResult> GetStatusAsync(
        ISptInstallationService sptInstall,
        IUpdateCheckService updateCheck,
        CancellationToken token)
    {
        await _statusLock.WaitAsync(token);
        try
        {
            string path = _args.Length > 0 ? _args[0] : Environment.CurrentDirectory;
            SemanticVersioning.Version? sptVer = await sptInstall.GetAndValidateSptVersionAsync(path, token);

            bool updateAvailable = false;
            string? latestAppVersion = null;
            if (sptVer != null)
            {
                CheckModsExtendedUpdateResult updateInfo = await updateCheck.CheckAsync(sptVer, token);
                updateAvailable = updateInfo.Status == CheckModsExtendedUpdateStatus.UpdateAvailable;
                latestAppVersion = updateInfo.LatestVersion;
            }

            return Results.Ok(new StatusResponse("running", VersionInfo.SemVer, sptVer?.ToString(), latestAppVersion, updateAvailable));
        }
        catch (Exception ex)
        {
            return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status500InternalServerError);
        }
        finally
        {
            _statusLock.Release();
        }
    }

    private static async Task<IResult> GetCacheAsync(IScanCacheService cacheService, CancellationToken token)
    {
        try
        {
            ScanCacheRecord? cache = await cacheService.LoadCacheAsync(token);
            if (cache != null)
            {
                return Results.Ok(cache);
            }
            return Results.NotFound(new ErrorResponse("No cache available"));
        }
        catch (Exception ex)
        {
            return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> PostScanAsync(IUpdateWorkflowOrchestrator orchestrator, CancellationToken token)
    {
        try
        {
            UpdateWorkflowContext context = await orchestrator.RunPipelineAsync(_args, token);
            ScanResponse response = ScanResponseMapper.Map(context);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> PostIgnoreAsync(IIgnoreService ignoreService, HttpRequest request, CancellationToken token)
    {
        try
        {
            IgnoreRequest? req = await request.ReadFromJsonAsync(
                CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, 
                cancellationToken: token) as IgnoreRequest;
            if (req != null && req.Id > 0 && !string.IsNullOrEmpty(req.LocalVersion) && !string.IsNullOrEmpty(req.LatestVersion))
            {
                await ignoreService.AddIgnoreAsync(req.Id, req.LocalVersion, req.LatestVersion, token);
                return Results.Ok(new MessageResponse($"Ignored {req.Id}"));
            }
            return Results.BadRequest(new ErrorResponse("Missing or invalid mod parameters"));
        }
        catch (Exception ex)
        {
            return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetIgnoresAsync(IIgnoreService ignoreService, CancellationToken token)
    {
        try
        {
            System.Collections.Generic.IReadOnlyList<IgnoredUpdate> existing = await ignoreService.GetIgnoresAsync(token);
            return Results.Ok(existing);
        }
        catch (Exception ex)
        {
            return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> DeleteIgnoreAsync(int modId, IIgnoreService ignoreService, CancellationToken token)
    {
        try
        {
            await ignoreService.RemoveIgnoreAsync(modId, token);
            return Results.Ok(new MessageResponse($"Removed ignore for {modId}"));
        }
        catch (Exception ex)
        {
            return Results.Json(new ErrorResponse(ex.Message), statusCode: StatusCodes.Status500InternalServerError);
        }
    }



    private static IResult PostSystemOpen(OpenSystemRequest req, IBrowserLauncher browserLauncher)
    {
        if (string.IsNullOrWhiteSpace(req.Target))
        {
            return Results.BadRequest(new ErrorResponse("Target is required"));
        }

        OneOf.OneOf<OneOf.Types.Success, ApiError> result = browserLauncher.TryOpenUrl(req.Target);
        return result.Match(
            success => Results.Ok(new MessageResponse("Opened target")),
            apiError => Results.BadRequest(new ErrorResponse(apiError.Message))
        );
    }
}
