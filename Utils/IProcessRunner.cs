using System.Diagnostics;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Utils;

/// <summary>
/// Abstraction for process execution.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Starts a process using the specified start information.
    /// </summary>
    /// <param name="startInfo">The start information to use to start the process.</param>
    /// <returns>The started process, or null.</returns>
    Process? Start(ProcessStartInfo startInfo);
}

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/>.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public Process? Start(ProcessStartInfo startInfo)
    {
        return Process.Start(startInfo);
    }
}
