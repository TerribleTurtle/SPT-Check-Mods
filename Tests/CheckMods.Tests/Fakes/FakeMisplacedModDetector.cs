using System.Collections.Generic;
using System.Threading;
using CheckMods.Models;
using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

public sealed class FakeMisplacedModDetector : IMisplacedModDetector
{
    public MisplacedModReport ReportToReturn { get; set; } = new MisplacedModReport([], []);

    public MisplacedModReport DetectMisplacedMods(string sptPath, CancellationToken cancellationToken = default)
    {
        return ReportToReturn;
    }
}






