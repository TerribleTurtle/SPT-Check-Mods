using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IModScannerService"/>.
/// </summary>
public sealed class FakeModScannerService : IModScannerService
{
    private List<Mod> _serverModsToReturn = [];
    private List<Mod> _clientModsToReturn = [];

    /// <summary> Gets or sets server mods. </summary>
    public List<Mod> ServerModsToReturn
    {
        get { return _serverModsToReturn.ToList(); }
        set { _serverModsToReturn = value.ToList(); }
    }

    /// <summary> Gets or sets client mods. </summary>
    public List<Mod> ClientModsToReturn
    {
        get { return _clientModsToReturn.ToList(); }
        set { _clientModsToReturn = value.ToList(); }
    }

    /// <summary> Gets or sets SPT version. </summary>
    public string? SptVersionToReturn { get; set; }

    /// <summary> Gets or sets misplaced mod report. </summary>
    public MisplacedModReport MisplacedModReportToReturn { get; set; } = new([], []);

    /// <inheritdoc />
    public Task<List<Mod>> ScanServerModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_serverModsToReturn.ToList());
    }

    /// <inheritdoc />
    public Task<List<Mod>> ScanClientModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_clientModsToReturn.ToList());
    }

    /// <inheritdoc />
    public Task<(List<Mod> ServerMods, List<Mod> ClientMods)> ScanAllModsAsync(
        string sptPath,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((_serverModsToReturn.ToList(), _clientModsToReturn.ToList()));
    }

    /// <inheritdoc />
    public string? GetSptVersion(string sptPath)
    {
        return SptVersionToReturn;
    }

    /// <inheritdoc />
    public Task<MisplacedModReport> DetectMisplacedModsAsync(
        string sptPath,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(MisplacedModReportToReturn);
    }
}
