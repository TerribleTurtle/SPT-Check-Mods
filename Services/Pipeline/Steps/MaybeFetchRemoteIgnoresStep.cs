using System;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheckModsExtended.Services.Pipeline.Steps;

/// <summary>
/// Workflow step that offers to fetch remote ignores.
/// </summary>
public sealed class MaybeFetchRemoteIgnoresStep(
    IRemoteIgnoreFileClient remoteIgnoreFileClient,
    IIgnoredUpdateStore ignoredUpdateStore,
    IModCheckReporter reporter,
    ILogger<MaybeFetchRemoteIgnoresStep> logger,
    IOptions<IgnoredUpdateOptions> options,
    ISettingsService settingsService
) : IWorkflowStep
{
    /// <inheritdoc />
    public async Task ExecuteAsync(UpdateWorkflowContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Offering remote ignore list refresh");

        if (Console.IsInputRedirected || !remoteIgnoreFileClient.IsConfigured)
        {
            return;
        }

        if (options.Value.UseCommunityList == false)
        {
            return;
        }

        if (options.Value.UseCommunityList == true)
        {
            var remoteSilent = await remoteIgnoreFileClient.FetchAsync(cancellationToken);
            if (remoteSilent is not null)
            {
                await ignoredUpdateStore.SyncRemoteIgnoresAsync(remoteSilent, cancellationToken);
            }
            return;
        }

        reporter.Blank();
        reporter.Heading("Community ignore list...");

        if (reporter.PromptFetchRemoteIgnores())
        {
            await settingsService.UpdateIgnoredUpdateOptionsAsync(true, cancellationToken);
            var remote = await remoteIgnoreFileClient.FetchAsync(cancellationToken);
            if (remote is null)
            {
                reporter.RemoteIgnoresUnavailable();
            }
            else
            {
                reporter.RemoteIgnoresMerged(
                    await ignoredUpdateStore.SyncRemoteIgnoresAsync(remote, cancellationToken)
                );
            }
        }
        else
        {
            await settingsService.UpdateIgnoredUpdateOptionsAsync(false, cancellationToken);
        }

        reporter.Blank();
        reporter.Rule();
    }
}
