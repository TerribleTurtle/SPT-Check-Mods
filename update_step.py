import os

base_dir = r'C:\Users\evanw\.gemini\antigravity\brain\8820e550-52fe-46e2-95e7-47af674114b7\.system_generated\worktrees\subagent-Data-Encapsulator-self-8517cb71'

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

# Services/Pipeline/Steps/ScanAndReconcileModsStep.cs
step_path = os.path.join(base_dir, 'Services', 'Pipeline', 'Steps', 'ScanAndReconcileModsStep.cs')
step = read_file(step_path)
step = step.replace('''public sealed class ScanAndReconcileModsStep(
    IModScannerService scannerService,
    ILocalModReconciler reconcilerService,
    IModCheckReporter reporter,
    ILogger<ScanAndReconcileModsStep> logger)''', '''public sealed class ScanAndReconcileModsStep(
    IModScannerService scannerService,
    ILocalModReconciler reconcilerService,
    IModCheckReporter reporter,
    ILogger<ScanAndReconcileModsStep> logger,
    IMisplacedModAnalyzerService misplacedModAnalyzer)''')
step = step.replace('var excludedFiles = new HashSet<string>(report.ExcludedFilePaths, StringComparer.OrdinalIgnoreCase);', 'var excludedFiles = new HashSet<string>(misplacedModAnalyzer.GetExcludedFilePaths(report), StringComparer.OrdinalIgnoreCase);')
step = step.replace('var excludedDirs = new HashSet<string>(report.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);', 'var excludedDirs = new HashSet<string>(misplacedModAnalyzer.GetExcludedDirectories(report), StringComparer.OrdinalIgnoreCase);')
write_file(step_path, step)
