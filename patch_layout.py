import sys

layout_css = 'wwwroot/css/layout.css'
with open(layout_css, 'a', encoding='utf-8') as f:
    f.write('''
/* More Utilities */
.items-center { align-items: center; }
.justify-between { justify-content: space-between; }
.flex-col { flex-direction: column; }
.gap-md { gap: var(--space-md); }
.text-muted { color: var(--text-muted); }
.text-warning { color: var(--status-warning); }
.text-error { color: var(--status-error); }
.text-success { color: var(--status-success); }
.text-info { color: var(--status-info); }
.font-semibold { font-weight: 600; }
.opacity-50 { opacity: 0.5; }
.text-sm { font-size: var(--text-sm); }
.mt-20 { margin-top: 20px; }
.ml-2 { margin-left: 8px; }
.ml-1 { margin-left: 5px; }
''')

components_css = 'wwwroot/css/components.css'
with open(components_css, 'a', encoding='utf-8') as f:
    f.write('''
/* Status Dots */
.status-dot {
    border-radius: 50%;
    width: 12px; height: 12px; min-width: 12px;
}
.status-dot.status-ok { box-shadow: 0 0 5px var(--status-success); }
.status-dot.status-update { box-shadow: 0 0 5px var(--status-warning); }
.status-dot.status-blocked { box-shadow: 0 0 5px var(--status-error); }
.status-dot.status-incompat { box-shadow: 0 0 5px var(--status-error); }
.status-dot.status-newer { box-shadow: 0 0 5px var(--status-info); }
.status-dot.status-unknown { box-shadow: 0 0 5px var(--status-neutral); }

/* Dashboard List */
.dashboard-list {
    list-style: none; padding: 0; display: flex; flex-direction: column; gap: var(--space-sm);
}
.dashboard-list-item {
    display: flex; justify-content: space-between; align-items: center;
    background: var(--bg-elevated); padding: var(--space-md);
    border-radius: var(--radius-sm); border: 1px solid var(--border-default);
}
.dashboard-summary-box {
    flex: 1; padding: 15px; border-radius: var(--radius-md); border: 1px solid;
}
.dashboard-summary-success { background: var(--status-success-bg); border-color: var(--status-success); color: var(--status-success); }
.dashboard-summary-warning { background: var(--status-warning-bg); border-color: var(--status-warning); color: var(--text-primary); }
''')
