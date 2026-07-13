import os

base_dir = r'C:\Users\evanw\.gemini\antigravity\brain\8820e550-52fe-46e2-95e7-47af674114b7\.system_generated\worktrees\subagent-Data-Encapsulator-self-8517cb71'

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

# Tests/CheckModsExtended.Tests/Models/MisplacedModReportTests.cs -> MisplacedModAnalyzerServiceTests.cs
test_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Models', 'MisplacedModReportTests.cs')
test = read_file(test_path)
test = test.replace('namespace CheckModsExtended.Tests.Models;', 'namespace CheckModsExtended.Tests.Services;\nusing CheckModsExtended.Services;')
test = test.replace('public sealed class MisplacedModReportTests', 'public sealed class MisplacedModAnalyzerServiceTests')
test = test.replace('Assert.Empty(report.ExcludedFilePaths);', 'var analyzer = new MisplacedModAnalyzerService();\n        Assert.Empty(analyzer.GetExcludedFilePaths(report));')
test = test.replace('Assert.Empty(report.ExcludedDirectories);', 'Assert.Empty(analyzer.GetExcludedDirectories(report));')

test = test.replace('Assert.Equal(new[] { serverInClient.FilePath, clientInServer.FilePath }, report.ExcludedFilePaths);', 'var analyzer = new MisplacedModAnalyzerService();\n        Assert.Equal(new[] { serverInClient.FilePath, clientInServer.FilePath }, analyzer.GetExcludedFilePaths(report));')
test = test.replace('Assert.Contains(intruder.FilePath, report.ExcludedFilePaths);', 'var analyzer = new MisplacedModAnalyzerService();\n        Assert.Contains(intruder.FilePath, analyzer.GetExcludedFilePaths(report));')
test = test.replace('Assert.DoesNotContain(legitimate.FilePath, report.ExcludedFilePaths);', 'Assert.DoesNotContain(legitimate.FilePath, analyzer.GetExcludedFilePaths(report));')
test = test.replace('Assert.Equal(new[] { directory.Directory }, report.ExcludedDirectories);', 'var analyzer = new MisplacedModAnalyzerService();\n        Assert.Equal(new[] { directory.Directory }, analyzer.GetExcludedDirectories(report));')
test = test.replace('Assert.DoesNotContain(modA.FilePath, report.ExcludedFilePaths);', 'Assert.DoesNotContain(modA.FilePath, analyzer.GetExcludedFilePaths(report));')
test = test.replace('Assert.DoesNotContain(modB.FilePath, report.ExcludedFilePaths);', 'Assert.DoesNotContain(modB.FilePath, analyzer.GetExcludedFilePaths(report));')

new_test_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Services', 'MisplacedModAnalyzerServiceTests.cs')
write_file(new_test_path, test)
os.remove(test_path)
