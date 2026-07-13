import os

base_dir = r'C:\Users\evanw\.gemini\antigravity\brain\8820e550-52fe-46e2-95e7-47af674114b7\.system_generated\worktrees\subagent-Data-Encapsulator-self-8517cb71'

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

# 1. Fix using directives
files = [
    os.path.join(base_dir, 'Services', 'Interfaces', 'IMisplacedModAnalyzerService.cs'),
    os.path.join(base_dir, 'Services', 'Interfaces', 'IModLinkResolverService.cs')
]
for p in files:
    content = read_file(p)
    lines = content.split('\n')
    usings = []
    others = []
    for line in lines:
        if line.startswith('using '):
            usings.append(line)
        else:
            others.append(line)
    new_content = '\n'.join(usings) + '\n\n' + '\n'.join(others).strip() + '\n'
    write_file(p, new_content)

# 2. Check ScanAndReconcileModsStep.cs
step_path = os.path.join(base_dir, 'Services', 'Pipeline', 'Steps', 'ScanAndReconcileModsStep.cs')
step = read_file(step_path)
step = step.replace('var excludedFiles = new HashSet<string>(report.ExcludedFilePaths, StringComparer.OrdinalIgnoreCase);', 'var excludedFiles = new HashSet<string>(misplacedModAnalyzer.GetExcludedFilePaths(report), StringComparer.OrdinalIgnoreCase);')
step = step.replace('var excludedDirs = new HashSet<string>(report.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);', 'var excludedDirs = new HashSet<string>(misplacedModAnalyzer.GetExcludedDirectories(report), StringComparer.OrdinalIgnoreCase);')

# Wait, the injection parameter might have been missed if my previous script failed or didn't replace correctly.
# Let's ensure misplacedModAnalyzer is added to the constructor.
# And also add CheckModsExtended.Services.Interfaces to usings if missing.
step = step.replace('IModCheckReporter reporter,\n    ILogger<ScanAndReconcileModsStep> logger)', 'IModCheckReporter reporter,\n    ILogger<ScanAndReconcileModsStep> logger,\n    IMisplacedModAnalyzerService misplacedModAnalyzer)')
write_file(step_path, step)
