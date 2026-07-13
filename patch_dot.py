import re

tb_path = 'wwwroot/js/ui/table.js'
with open(tb_path, 'r', encoding='utf-8') as f:
    tb = f.read()

tb = re.sub(r'style="border-radius: 50%; box-shadow:[^"]+"', 'class="status-dot status-"', tb)

with open(tb_path, 'w', encoding='utf-8') as f:
    f.write(tb)
