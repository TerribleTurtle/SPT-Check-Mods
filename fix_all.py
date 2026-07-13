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
     'SemanticVersioning.Version? sptVer = await sptInstall.GetAndValidateSptVersionAsync(path, token);'),
    ('var updateInfo = await updateCheck.CheckAsync(sptVer, token);',
     'CheckModsExtendedUpdateResult updateInfo = await updateCheck.CheckAsync(sptVer, token);'),
    ('var cache = await cacheService.LoadCacheAsync(token);',
     'ScanCacheRecord? cache = await cacheService.LoadCacheAsync(token);'),
    ('var context = await orchestrator.RunPipelineAsync(_args, token);',
     'UpdateWorkflowContext context = await orchestrator.RunPipelineAsync(_args, token);'),
    ('var response = ScanResponseMapper.Map(context);',
     'ScanResponse response = ScanResponseMapper.Map(context);'),
    ('var req = await request.ReadFromJsonAsync(CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, cancellationToken: token) as IgnoreRequest;',
     'IgnoreRequest? req = await request.ReadFromJsonAsync(CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, cancellationToken: token) as IgnoreRequest;'),
    ('var existing = await ignoreService.GetIgnoresAsync(token);',
     'System.Collections.Generic.IReadOnlyList<IgnoredUpdate> existing = await ignoreService.GetIgnoresAsync(token);'),
    ('var result = browserLauncher.TryOpenUrl(req.Target);',
     'OneOf.OneOf<OneOf.Types.Success, CheckModsExtended.Models.ApiError> result = browserLauncher.TryOpenUrl(req.Target);')
])
print('Patched WebEndpoints.cs')

patch('Services/SettingsService.cs', [
    ('public class SettingsService : ISettingsService',
     '/// <summary>\n/// Service for managing settings via the file system.\n/// </summary>\npublic sealed class SettingsService : ISettingsService'),
    ('    public SettingsService(IFileSystem fileSystem)',
     '    /// <summary>\n    /// Initializes a new instance of the SettingsService class.\n    /// </summary>\n    /// <param name=\"fileSystem\">The file system abstraction.</param>\n    public SettingsService(IFileSystem fileSystem)'),
    ('    public async Task<string> GetSettingsAsync(CancellationToken token = default)',
     '    /// <summary>\n    /// Gets the settings content as a JSON string.\n    /// </summary>\n    /// <param name=\"token\">Cancellation token.</param>\n    public async Task<string> GetSettingsAsync(CancellationToken token = default)'),
    ('    public async Task<OneOf<MessageResponse, ApiError>> UpdateSettingsAsync(string jsonPayload, CancellationToken token = default)',
     '    /// <summary>\n    /// Updates the settings file with the provided JSON payload.\n    /// </summary>\n    /// <param name=\"jsonPayload\">The JSON settings payload.</param>\n    /// <param name=\"token\">Cancellation token.</param>\n    public async Task<OneOf<MessageResponse, ApiError>> UpdateSettingsAsync(string jsonPayload, CancellationToken token = default)'),
    ('var path = \"appsettings.json\";',
     'string path = \"appsettings.json\";')
])
print('Patched SettingsService.cs')

patch('Tests/CheckModsExtended.Tests/Services/SettingsServiceTests.cs', [
    ('public class SettingsServiceTests',
     'public sealed class SettingsServiceTests'),
    ('var exampleJson =', 'string exampleJson ='),
    ('var result = await _settingsService.GetSettingsAsync();', 'string result = await _settingsService.GetSettingsAsync();'),
    ('var validJson =', 'string validJson ='),
    ('var result = await _settingsService.UpdateSettingsAsync(validJson);', 'OneOf.OneOf<CheckModsExtended.Models.MessageResponse, CheckModsExtended.Models.ApiError> result = await _settingsService.UpdateSettingsAsync(validJson);'),
    ('var writtenContent =', 'string writtenContent ='),
    ('var invalidJson =', 'string invalidJson ='),
    ('var result = await _settingsService.UpdateSettingsAsync(invalidJson);', 'OneOf.OneOf<CheckModsExtended.Models.MessageResponse, CheckModsExtended.Models.ApiError> result = await _settingsService.UpdateSettingsAsync(invalidJson);')
])
print('Patched SettingsServiceTests.cs')
