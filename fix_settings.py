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

patch('Services/Web/SettingsEndpoints.cs', [
    ('using Microsoft.AspNetCore.Http;\nusing Microsoft.AspNetCore.Routing;\n\nnamespace CheckModsExtended.Services.Web;\n\npublic static class SettingsEndpoints',
     'using Microsoft.AspNetCore.Http;\nusing Microsoft.AspNetCore.Routing;\nusing OneOf;\n\nnamespace CheckModsExtended.Services.Web;\n\n/// <summary>\n/// Registers the REST API endpoints for settings management.\n/// </summary>\npublic static class SettingsEndpoints'),
    ('    public static void MapSettingsEndpoints(RouteGroupBuilder api)',
     '    /// <summary>\n    /// Maps the settings endpoints to the provided route group builder.\n    /// </summary>\n    /// <param name=\"api\">The route group builder.</param>\n    public static void MapSettingsEndpoints(RouteGroupBuilder api)'),
    ('var settings = await settingsService.GetSettingsAsync(token);',
     'string settings = await settingsService.GetSettingsAsync(token);'),
    ('using var reader = new StreamReader(request.Body);',
     'using StreamReader reader = new StreamReader(request.Body);'),
    ('var content = await reader.ReadToEndAsync(token);',
     'string content = await reader.ReadToEndAsync(token);'),
    ('var result = await settingsService.UpdateSettingsAsync(content, token);',
     'OneOf<MessageResponse, ApiError> result = await settingsService.UpdateSettingsAsync(content, token);')
])
print('Patched SettingsEndpoints.cs')
