using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Caches the results of the pipeline scan for fast cold-starts.
/// </summary>
public sealed class CacheResultsStep : IWorkflowStep
{
    private readonly IScanCacheService _cacheService;

    public CacheResultsStep(IScanCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        if (context.IsCancelled)
        {
            return;
        }

        var response = ScanResponseMapper.Map(context);
        var record = new ScanCacheRecord(DateTimeOffset.UtcNow, context.SptPath, response);
        await _cacheService.SaveCacheAsync(record, cancellationToken);
    }
}
