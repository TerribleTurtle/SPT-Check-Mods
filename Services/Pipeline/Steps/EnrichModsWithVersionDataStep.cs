using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;


namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that enriches mods with version data.
/// </summary>

public sealed class EnrichModsWithVersionDataStep(
    IModEnrichmentService modEnrichmentService,
    ILogger<EnrichModsWithVersionDataStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Enriching mods with version data");

        var matchedMods = context.Mods.Where(m => m.IsMatched).ToList();

        if (matchedMods.Count == 0)
        {
            return;
        }

        var enrichedMods = await modEnrichmentService.EnrichAllWithVersionDataAsync(
            matchedMods, context.SptVersion!, cancellationToken
        );

        var enrichedByGuid = enrichedMods.ToDictionary(m => m.Local.Guid, StringComparer.OrdinalIgnoreCase);

        var result = new List<Mod>(context.Mods.Count);
        foreach (var mod in context.Mods)
        {
            if (!string.IsNullOrWhiteSpace(mod.Local.Guid) && 
                enrichedByGuid.TryGetValue(mod.Local.Guid, out var enriched))
            {
                result.Add(enriched);
            }
            else
            {
                result.Add(mod);
            }
        }
        
        context.Mods = result;
    }
}

