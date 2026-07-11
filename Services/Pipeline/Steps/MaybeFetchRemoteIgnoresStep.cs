using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;


namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that offers to fetch remote ignores.
/// </summary>

public sealed class MaybeFetchRemoteIgnoresStep(
    IRemoteIgnoreFileClient remoteIgnoreFileClient,
    IIgnoredUpdateStore ignoredUpdateStore,
    IModCheckReporter reporter,
    ILogger<MaybeFetchRemoteIgnoresStep> logger) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Offering remote ignore list refresh");

        if (Console.IsInputRedirected || !remoteIgnoreFileClient.IsConfigured)
        {
            return;
        }

        reporter.Blank();
        reporter.Heading("Community ignore list...");

        if (reporter.PromptFetchRemoteIgnores())
        {
            var remote = await remoteIgnoreFileClient.FetchAsync(cancellationToken);
            if (remote is null)
            {
                reporter.RemoteIgnoresUnavailable();
            }
            else
            {
                reporter.RemoteIgnoresMerged(await ignoredUpdateStore.MergeWithoutOverwriteAsync(remote, cancellationToken));
            }
        }

        reporter.Blank();
        reporter.Rule();
    }
}

