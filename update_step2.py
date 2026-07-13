import os

base_dir = r'C:\Users\evanw\.gemini\antigravity\brain\8820e550-52fe-46e2-95e7-47af674114b7\.system_generated\worktrees\subagent-Data-Encapsulator-self-8517cb71'

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

step_path = os.path.join(base_dir, 'Services', 'Pipeline', 'Steps', 'ScanAndReconcileModsStep.cs')
step = read_file(step_path)

step = step.replace('''public sealed class ScanAndReconcileModsStep(
    IModScannerService modScannerService,
    IModResolutionService modResolutionService,
    IModReconciliationService modReconciliationService,
    IModCheckReporter reporter,
    ILogger<ScanAndReconcileModsStep> logger
) : IWorkflowStep''', '''public sealed class ScanAndReconcileModsStep(
    IModScannerService modScannerService,
    IModResolutionService modResolutionService,
    IModReconciliationService modReconciliationService,
    IModCheckReporter reporter,
    ILogger<ScanAndReconcileModsStep> logger,
    IMisplacedModAnalyzerService misplacedModAnalyzer
) : IWorkflowStep''')

step = step.replace('serverMods = ExcludeMisplacedMods(serverMods, context.MisplacedReport);', 'serverMods = ExcludeMisplacedMods(serverMods, context.MisplacedReport, misplacedModAnalyzer);')
step = step.replace('clientMods = ExcludeMisplacedMods(clientMods, context.MisplacedReport);', 'clientMods = ExcludeMisplacedMods(clientMods, context.MisplacedReport, misplacedModAnalyzer);')

step = step.replace('private static List<Mod> ExcludeMisplacedMods(List<Mod> mods, MisplacedModReport report)', 'private static List<Mod> ExcludeMisplacedMods(List<Mod> mods, MisplacedModReport report, IMisplacedModAnalyzerService misplacedModAnalyzer)')

step = step.replace('var excludedDirectories = report.ExcludedDirectories;', 'var excludedDirectories = misplacedModAnalyzer.GetExcludedDirectories(report);')

write_file(step_path, step)
