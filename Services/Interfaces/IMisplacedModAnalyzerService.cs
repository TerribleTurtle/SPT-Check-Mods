using System.Collections.Generic;
using CheckModsExtended.Models;

namespace CheckModsExtended.Services.Interfaces;

public interface IMisplacedModAnalyzerService
{
    IReadOnlyList<string> GetExcludedFilePaths(MisplacedModReport report);
    IReadOnlyList<string> GetExcludedDirectories(MisplacedModReport report);
}
