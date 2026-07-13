import os

base_dir = r'C:\Users\evanw\.gemini\antigravity\brain\8820e550-52fe-46e2-95e7-47af674114b7\.system_generated\worktrees\subagent-Data-Encapsulator-self-8517cb71'

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

enrich_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Services', 'ModEnrichmentServiceTests.cs')
enrich = read_file(enrich_path)
enrich = enrich.replace('_service = new ModEnrichmentService(_forgeApiService, _gitHubClient, _logger);', '_service = new ModEnrichmentService(_forgeApiService, _gitHubClient, _logger, new CheckModsExtended.Services.ModLinkResolverService());')
write_file(enrich_path, enrich)

scan_path = os.path.join(base_dir, 'Tests', 'CheckModsExtended.Tests', 'Pipeline', 'Steps', 'ScanAndReconcileModsStepTests.cs')
scan = read_file(scan_path)
scan = scan.replace('''        var step = new ScanAndReconcileModsStep(
            modScannerService,
            modResolutionService,
            modReconciliationService,
            reporter,
            logger
        );''', '''        var step = new ScanAndReconcileModsStep(
            modScannerService,
            modResolutionService,
            modReconciliationService,
            reporter,
            logger,
            new CheckModsExtended.Services.MisplacedModAnalyzerService()
        );''')
# also fix line 55ish if there is another constructor call
scan = scan.replace('''        var step = new ScanAndReconcileModsStep(
            scannerService,
            resolutionService,
            reconciliationService,
            reporter,
            logger
        );''', '''        var step = new ScanAndReconcileModsStep(
            scannerService,
            resolutionService,
            reconciliationService,
            reporter,
            logger,
            new CheckModsExtended.Services.MisplacedModAnalyzerService()
        );''')
write_file(scan_path, scan)
