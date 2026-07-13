import sys

def patch(file, replacements):
    with open(file, 'r', encoding='utf-8') as f:
        content = f.read()
    for old, new in replacements:
        if old in content:
            content = content.replace(old, new)
    with open(file, 'w', encoding='utf-8') as f:
        f.write(content)

patch('Services/Web/WebEndpoints.cs', [
    ('IgnoreRequest? req = await request.ReadFromJsonAsync(CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, cancellationToken: token) as IgnoreRequest;',
     'IgnoreRequest? req = await request.ReadFromJsonAsync(\n                CheckModsExtendedJsonSerializerContext.Default.IgnoreRequest, \n                cancellationToken: token) as IgnoreRequest;')
])
print('Wrapped long line')
