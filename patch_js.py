import sys
import re

# Add more utilities to layout.css
with open('wwwroot/css/layout.css', 'a', encoding='utf-8') as f:
    f.write('''
/* Even more utils */
.mb-2 { margin-bottom: 10px; }
.mt-2 { margin-top: 10px; }
.ml-5 { margin-left: 20px; }
.mx-1 { margin: 0 2px; }
.justify-center { justify-content: center; }
.flex-1 { flex: 1; }
.uppercase { text-transform: uppercase; }
.grid { display: grid; }
.grid-cols-1 { grid-template-columns: 1fr; }
''')

# Patch dashboard.js
db_path = 'wwwroot/js/ui/dashboard.js'
with open(db_path, 'r', encoding='utf-8') as f:
    db = f.read()

db_reps = [
    ('style="color: var(--text-muted); font-size: 0.9rem;"', 'class="text-muted text-sm"'),
    ('style="list-style: none; padding: 0; display: flex; flex-direction: column; gap: 8px;"', 'class="dashboard-list"'),
    ('style="display: flex; justify-content: space-between; align-items: center; background: var(--bg-elevated); padding: 10px; border-radius: var(--radius-sm); border: 1px solid var(--border-default);"', 'class="dashboard-list-item"'),
    ('style="color: var(--status-error); margin-top: 20px;"', 'class="text-error mt-20"'),
    ('style="flex: 1; background: var(--status-success-bg); border: 1px solid var(--status-success); color: var(--status-success); padding: 15px; border-radius: var(--radius-md);"', 'class="dashboard-summary-box dashboard-summary-success"'),
    ('style="margin-bottom: 10px;"', 'class="mb-2"'),
    ('style="flex: 1; background: var(--status-warning-bg); border: 1px solid var(--status-warning); color: var(--text-primary); padding: 15px; border-radius: var(--radius-md);"', 'class="dashboard-summary-box dashboard-summary-warning"'),
    ('style="color: var(--status-warning); margin-bottom: 10px;"', 'class="text-warning mb-2"'),
    ('style="margin-top:10px; margin-left: 20px;"', 'class="mt-2 ml-5"'),
    ('style="flex: 1; display: flex; flex-direction: column; justify-content: center;"', 'class="flex flex-col justify-center flex-1"'),
    ('style="color: var(--text-secondary); margin-bottom: 10px; text-transform: uppercase; font-size: 0.8rem;"', 'class="text-muted mb-2 uppercase text-sm"'),
    ('style="display: grid; grid-template-columns: 1fr; gap: 10px;"', 'class="grid grid-cols-1 gap-sm"'),
]

for old, new in db_reps:
    db = db.replace(old, new)

with open(db_path, 'w', encoding='utf-8') as f:
    f.write(db)

# Patch table.js
tb_path = 'wwwroot/js/ui/table.js'
with open(tb_path, 'r', encoding='utf-8') as f:
    tb = f.read()

tb_reps = [
    ('style="background-color: var(--status-warning); color: var(--text-dark); margin-left: 8px; font-size: 0.7rem; padding: 2px 6px; border-radius: 4px; font-weight: bold; vertical-align: middle;"', 'class="badge badge-warning ml-2"'),
    ('style="color: var(--status-warning); margin-left: 5px; font-size: 0.9rem;"', 'class="text-warning ml-1 text-sm"'),
    ('style="color: var(--status-success); font-weight: 600;"', 'class="text-success font-semibold"'),
    ('style="color: var(--status-info); font-weight: 600;"', 'class="text-info font-semibold"'),
    ('style="opacity:0.5; margin:0 2px;"', 'class="opacity-50 mx-1"'),
    ('style="display:flex; align-items:center; gap:var(--space-md);"', 'class="flex items-center gap-md"'),
    ('style="border-radius: 50%; box-shadow: 0 0 5px var(--status-); width: 12px; height: 12px; min-width: 12px;"', 'class="status-dot status-"'),
    ('style="font-weight: 600;"', 'class="font-semibold"'),
    ('style="text-align: right;"', 'class="text-right"'),
]

for old, new in tb_reps:
    tb = tb.replace(old, new)

with open(tb_path, 'w', encoding='utf-8') as f:
    f.write(tb)

print('Patched dashboard.js and table.js')
