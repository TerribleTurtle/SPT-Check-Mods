import sys

def patch(file, replacements):
    with open(file, 'r', encoding='utf-8') as f:
        content = f.read()
    for old, new in replacements:
        if old not in content:
            pass # ignore if already replaced
        else:
            content = content.replace(old, new)
    with open(file, 'w', encoding='utf-8') as f:
        f.write(content)

patch('Tests/CheckModsExtended.Tests/Services/SettingsServiceTests.cs', [
    ('OneOf.OneOf<CheckModsExtended.Models.MessageResponse, CheckModsExtended.Models.ApiError>',
     'OneOf.OneOf<MessageResponse, ApiError>')
])

patch('Services/Web/WebEndpoints.cs', [
    ('OneOf.OneOf<OneOf.Types.Success, CheckModsExtended.Models.ApiError>',
     'OneOf.OneOf<OneOf.Types.Success, ApiError>')
])
print('Patched namespace')

