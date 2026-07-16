using System.Collections.Generic;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class MisplacedModAnalyzerService : IMisplacedModAnalyzerService
{
    public IReadOnlyList<string> GetExcludedFilePaths(MisplacedModReport report)
    {
        return report.WrongFolder
            .Select(mod => mod.FilePath)
            .Concat(
                report.CrossInstalled
                    .Where(directory => !directory.Ambiguous)
                    .SelectMany(directory => directory.Misplaced)
                    .Select(mod => mod.FilePath)
            )
            .ToList();
    }

    public IReadOnlyList<string> GetExcludedDirectories(MisplacedModReport report)
    {
        return report.CrossInstalled.Where(directory => directory.Ambiguous).Select(directory => directory.Directory).ToList();
    }
}
