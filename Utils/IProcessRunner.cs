using System.Diagnostics;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Utils;

/// <summary>
/// Abstraction for process execution.
/// </summary>
public interface IProcessRunner
{
    Process? Start(ProcessStartInfo startInfo);
}

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/>.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ProcessRunner : IProcessRunner
{
    public Process? Start(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo);
    }
}
