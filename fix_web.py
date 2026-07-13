import sys

def patch(file, replacements):
    with open(file, 'r', encoding='utf-8') as f:
        content = f.read()
    for old, new in replacements:
        if old not in content:
            print(f'Missing old string in {file}:\n{old}')
            sys.exit(1)
        content = content.replace(old, new)
    with open(file, 'w', encoding='utf-8') as f:
        f.write(content)

patch('Services/Web/WebEndpoints.cs', [
    ('    public static void MapEndpoints(WebApplication app, string[] args)',
     '    /// <summary>\n    /// Maps the Web Manager endpoints to the provided application.\n    /// </summary>\n    /// <param name=\"app\">The web application.</param>\n    /// <param name=\"args\">Command line arguments.</param>\n    public static void MapEndpoints(WebApplication app, string[] args)'),
    ('var api = app.MapGroup(\"/api\");',
     'RouteGroupBuilder api = app.MapGroup(\"/api\");'),
    ('var path = _args.Length > 0 ? _args[0] : Environment.CurrentDirectory;',
     'string path = _args.Length > 0 ? _args[0] : Environment.CurrentDirectory;'),
    ('var sptVer = await sptInstall.GetAndValidateSptVersionAsync(path, token);',
     'System.Version? sptVer = await sptInstall.GetAndValidateSptVersionAsync(path, token);'),
    ('var updateInfo = await updateCheck.CheckAsync(sptVer, token);',
     'var updateInfo = await updateCheck.CheckAsync(sptVer, token); /* TODO: Fix type */'),
    ('var cache = await cacheService.LoadCacheAsync(token);',
     'ScanCache? cache = await cacheService.LoadCacheAsync(token);'),
    ('var context = await orchestrator.RunPipelineAsync(_args, token);',
     'PipelineContext context = await orchestrator.RunPipelineAsync(_args, token);'),
    ('var response = ScanResponseMapper.Map(context);',
     'ScanResponse response = ScanResponseMapper.Map(context);'),
    ('var req = await request.ReadFromJsonAsync(CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, cancellationToken: token) as IgnoreRequest;',
     'IgnoreRequest? req = await request.ReadFromJsonAsync(CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, cancellationToken: token) as IgnoreRequest;'),
    ('var existing = await ignoreService.GetIgnoresAsync(token);',
     'var existing = await ignoreService.GetIgnoresAsync(token); /* TODO: Fix type */'),
    ('var result = browserLauncher.TryOpenUrl(req.Target);',
     'var result = browserLauncher.TryOpenUrl(req.Target); /* TODO: Fix type */')
])
print('Patched WebEndpoints.cs partially')
