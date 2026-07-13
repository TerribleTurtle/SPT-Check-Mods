import os

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
enrich_tests = enrich_tests.replace(
    'var service = new ModEnrichmentService(_forgeApiClientMock.Object, _gitHubReleaseClientMock.Object, _loggerMock.Object);',
    'var service = new ModEnrichmentService(_forgeApiClientMock.Object, _gitHubReleaseClientMock.Object, _loggerMock.Object, new Moq.Mock<CheckModsExtended.Services.Interfaces.IModLinkResolverService>().Object);'
)
write_file(enrich_tests_path, enrich_tests)

# 2. ScanAndReconcileModsStepTests.cs
scan_tests_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Pipeline', 'Steps', 'ScanAndReconcileModsStepTests.cs')
scan_tests = read_file(scan_tests_path)
scan_tests = scan_tests.replace(
    '_loggerMock.Object',
    '_loggerMock.Object,\n            new Moq.Mock<CheckModsExtended.Services.Interfaces.IMisplacedModAnalyzerService>().Object'
)
write_file(scan_tests_path, scan_tests)

# 3. VersionTableUiRendererTests.cs
ui_tests_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Services', 'UI', 'VersionTableUiRendererTests.cs')
ui_tests = read_file(ui_tests_path)
ui_tests = ui_tests.replace(
    'mod = mod.WithUpToDate(new UpToDateMod(null, 123, null, null, "1.0.0", null));',
    'mod = mod.WithUpToDate(new UpToDateMod(null, 123, null, null, "1.0.0", null), null);'
)
write_file(ui_tests_path, ui_tests)

# 4. ModUpdateMethodsTests.cs
update_tests_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Models', 'ModUpdateMethodsTests.cs')
update_tests = read_file(update_tests_path)
update_tests = update_tests.replace(
    'mod = mod.WithUpToDate(new UpToDateMod(null, 2471, "com.author.coolmod", "Cool Mod", "1.0.0", null));',
    'mod = mod.WithUpToDate(new UpToDateMod(null, 2471, "com.author.coolmod", "Cool Mod", "1.0.0", null), null);'
)
update_tests = update_tests.replace(
    'mod = mod.WithIncompatible(\n            new IncompatibleMod(null, 2471, "com.author.coolmod", "Cool Mod", "1.0.0", "Requires SPT 3.8", null)\n        );',
    'mod = mod.WithIncompatible(\n            new IncompatibleMod(null, 2471, "com.author.coolmod", "Cool Mod", "1.0.0", "Requires SPT 3.8", null), null\n        );'
)
write_file(update_tests_path, update_tests)
