using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that removes any legacy API key file.
/// </summary>
public sealed class RemoveLegacyApiKeyStep(IInitializationService initializationService) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        await initializationService.RemoveLegacyApiKeyFileAsync();
    }
}
