using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CheckModsExtended.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly AppPaths _appPaths;

    public MaintenanceService(IOptions<AppPaths> appPaths)
    {
        _appPaths = appPaths.Value;
    }

    public Task<bool> CleanAppDataAsync(CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(_appPaths.AppDataDirectory))
        {
            Directory.Delete(_appPaths.AppDataDirectory, true);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
