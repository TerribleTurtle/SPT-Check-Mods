import sys
import re

filepath = 'wwwroot/index.html'
with open(filepath, 'r', encoding='utf-8') as f:
    content = f.read()

replacements = [
    ('style="font-family: var(--font-brand)"', 'class="font-brand"'),
    ('class="chip cached pulsing-cache" style="display: none; background-color: var(--status-warning); color: var(--bg-primary); margin-right: 1rem; border-color: var(--status-warning);"', 'class="chip cached pulsing-cache badge badge-warning hidden"'),
    ('style="margin-right: 4px; vertical-align: text-bottom;"', 'class="mr-1 align-text-bottom"'),
    ('style="display: none;"', 'class="hidden"'),
    ('style="position: relative;"', 'class="relative"'),
    ('style="width: 100%;"', 'class="w-full"'),
    ('style="width: 40px; text-align: left;"', 'class="text-left w-40px"'),
    ('style="text-align: left;"', 'class="text-left"'),
    ('style="text-align: right;"', 'class="text-right"'),
    ('style="color: var(--status-warning);"', 'class="text-warning"'),
    ('style="margin-left: auto;"', 'class="ml-auto"'),
]

for old, new in replacements:
    content = content.replace(old, new)

with open(filepath, 'w', encoding='utf-8') as f:
    f.write(content)

print('Patched index.html')
