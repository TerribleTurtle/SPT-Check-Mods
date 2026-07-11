using System;
using System.Threading.Tasks;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.UI;

/// <summary>
/// Spectre.Console implementation of <see cref="IProgressRenderer"/>.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class ProgressRenderer : IProgressRenderer
{
    /// <inheritdoc />
    public async Task RunForgeQueryProgressAsync(int total, Func<Action<int>, Task> work, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await CreateForgeProgress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[grey]Querying Forge API[/]", maxValue: total);
                await work(current => task.Value = current);
                task.StopTask();
            });
    }

    /// <inheritdoc />
    public async Task<T> RunForgeQueryProgressAsync<T>(int total, Func<Action<int>, Task<T>> work, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await CreateForgeProgress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[grey]Querying Forge API[/]", maxValue: total);
                var result = await work(current => task.Value = current);
                task.StopTask();
                return result;
            });
    }

    private static Progress CreateForgeProgress()
    {
        return AnsiConsole
            .Progress()
            .Columns(
                new SpinnerColumn(Spinner.Known.Dots) { Style = Style.Parse("blue") },
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn()
            );
    }
}
