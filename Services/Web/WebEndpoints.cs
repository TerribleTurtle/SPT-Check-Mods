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
                SemanticVersioning.Version? sptVer = null;
                try { 
                    var path = args.Length > 0 ? args[0] : System.Environment.CurrentDirectory;
                    sptVer = await sptInstall.GetAndValidateSptVersionAsync(path, token); 
                } catch { }
                
                bool updateAvailable = false;
                string? latestAppVersion = null;
                if (sptVer != null) {
                    try {
                        var updateInfo = await updateCheck.CheckAsync(sptVer, token);
                        updateAvailable = updateInfo.Status == CheckModsExtended.Models.CheckModsExtendedUpdateStatus.UpdateAvailable;
                        latestAppVersion = updateInfo.LatestVersion;
                    } catch { }
                }
                
                return Results.Ok(new StatusResponse("running", CheckModsExtended.Utils.VersionInfo.SemVer, sptVer?.ToString(), latestAppVersion, updateAvailable));
            }
            finally
            {
                _statusLock.Release();
            }
        });
        
        api.MapPost("/scan", async (CheckModsExtended.Services.Interfaces.IUpdateWorkflowOrchestrator orchestrator, CancellationToken token) => 
        {
            var context = await orchestrator.RunPipelineAsync(args, token);
            
            var response = context.Mods.Select(m => new ModDto(
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
                )).ToList(),
                SourceCodeUrl: m.Api.ApiSourceCodeUrl,
                LocalSptVersion: m.Local.LocalSptVersion,
                HasWarnings: m.HasWarnings,
                LoadWarnings: m.LoadWarnings.Count > 0 ? m.LoadWarnings.ToList() : null,
                IsIgnored: m.Update.UpdateSuppressed,
                IsPaired: m.Local.PairedComponentPath != null,
                LocalDirectory: m.Local.FilePath != null ? System.IO.Path.GetDirectoryName(m.Local.FilePath) : null
            )).ToList();
            
            MisplacedModReportDto? misplacedReportDto = null;
            if (context.MisplacedReport != null && context.MisplacedReport.Any)
            {
                misplacedReportDto = new MisplacedModReportDto(
                    WrongFolder: context.MisplacedReport.WrongFolder.Select(m => new MisplacedModDto(
                        Name: m.Name,
                        Version: m.Version,
                        FilePath: m.FilePath,
                        IsServerMod: m.IsServerMod
                    )).ToList(),
                    CrossInstalled: context.MisplacedReport.CrossInstalled.Select(d => new CrossInstalledDirectoryDto(
                        Directory: d.Directory,
                        Mods: d.Mods.Select(m => new MisplacedModDto(
                            Name: m.Name,
                            Version: m.Version,
                            FilePath: m.FilePath,
                            IsServerMod: m.IsServerMod
                        )).ToList(),
                        Ambiguous: d.Ambiguous
                    )).ToList()
                );
            }
            
            return Results.Ok(new ScanResponse(response, misplacedReportDto, context.SptVersion?.ToString()));
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
