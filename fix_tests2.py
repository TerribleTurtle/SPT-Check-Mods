import os
import re

base_dir = r'C:\Users\evanw\.gemini\antigravity\brain\8820e550-52fe-46e2-95e7-47af674114b7\.system_generated\worktrees\subagent-Data-Encapsulator-self-8517cb71'

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

# 1. ModEnrichmentServiceTests.cs
enrich_tests_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Services', 'ModEnrichmentServiceTests.cs')
enrich_tests = read_file(enrich_tests_path)
enrich_tests = re.sub(
    r'(var service = new ModEnrichmentService\(_forgeApiClientMock\.Object, _gitHubReleaseClientMock\.Object, _loggerMock\.Object)\);',
    r'\1, new Moq.Mock<CheckModsExtended.Services.Interfaces.IModLinkResolverService>().Object);',
    enrich_tests
)
write_file(enrich_tests_path, enrich_tests)

# 2. ScanAndReconcileModsStepTests.cs
scan_tests_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Pipeline', 'Steps', 'ScanAndReconcileModsStepTests.cs')
scan_tests = read_file(scan_tests_path)
scan_tests = re.sub(
    r'(_loggerMock\.Object\s*)\);',
    r'\1, new Moq.Mock<CheckModsExtended.Services.Interfaces.IMisplacedModAnalyzerService>().Object);',
    scan_tests
)
write_file(scan_tests_path, scan_tests)

# 3. ModUpdateMethodsTests.cs
update_tests_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Models', 'ModUpdateMethodsTests.cs')
update_tests = read_file(update_tests_path)
update_tests = re.sub(
    r'(mod = mod\.WithIncompatible\(\s*new IncompatibleMod\([^)]+\)\s*)\);',
    r'\1, null);',
    update_tests
)
write_file(update_tests_path, update_tests)
