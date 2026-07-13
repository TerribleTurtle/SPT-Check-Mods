using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;

namespace CheckModsExtended.Tests.Fakes;

public sealed class TestModScannerService(IModScannerService inner) : IModScannerService
{
    public Task<(List<Mod> ServerMods, List<Mod> ClientMods)> ScanAllModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        return inner.ScanAllModsAsync(sptPath, cancellationToken);
    }

    public Task<List<Mod>> ScanServerModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        return inner.ScanServerModsAsync(sptPath, cancellationToken);
    }

    public Task<List<Mod>> ScanClientModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        return inner.ScanClientModsAsync(sptPath, cancellationToken);
    }

    public string? GetSptVersion(string sptPath)
    {
        return "3.8.0";
    }

    public Task<MisplacedModReport> DetectMisplacedModsAsync(string sptPath, CancellationToken cancellationToken = default)
    {
        return inner.DetectMisplacedModsAsync(sptPath, cancellationToken);
    }
}
