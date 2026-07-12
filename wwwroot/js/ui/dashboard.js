import { state } from '../state.js';
import { fetchIgnores, systemOpen } from '../api.js';
import { escapeHtml, logToConsole } from '../utils.js';

export function renderBulkBar(selectedIds) {
    const bulkBar = document.getElementById('bulk-bar');
    const bulkCount = document.getElementById('bulk-count');
    if (!bulkBar || !bulkCount) return;

    if (selectedIds.size > 0) {
        bulkBar.hidden = false;
        bulkCount.textContent = `${selectedIds.size} selected`;
    } else {
        bulkBar.hidden = true;
    }
}

export async function renderIgnoreDashboard() {
    try {
        const ignores = await fetchIgnores();
        
        let html = '';
        if (!ignores || ignores.length === 0) {
            html += '<p style="color: var(--text-muted); font-size: 0.9rem;">No mods are currently ignored.</p>';
        } else {
            html += '<ul style="list-style: none; padding: 0; display: flex; flex-direction: column; gap: 8px;">';
            ignores.forEach(ig => {
                html += `<li style="display: flex; justify-content: space-between; align-items: center; background: var(--bg-elevated); padding: 10px; border-radius: var(--radius-sm); border: 1px solid var(--border-default);">
                    <span><strong>${escapeHtml(ig.name || ig.id)}</strong> (v${escapeHtml(ig.localVersion)})</span>
                    <button class="btn-secondary action-unignore" data-id="${escapeHtml(ig.id)}">Remove</button>
                </li>`;
            });
            html += '</ul>';
        }
        
        const modalBody = document.getElementById('ignore-modal-body');
        if (modalBody) {
            modalBody.innerHTML = html;
        }
        return html;
    } catch (e) {
        const modalBody = document.getElementById('ignore-modal-body');
        if (modalBody) {
            modalBody.innerHTML = `<div style="color: var(--status-error); margin-top: 20px;">Error loading ignores: ${e.message}</div>`;
        }
    }
}

export async function showOverview() {
    const overviewPane = document.getElementById('workspace-overview');
    const detailTitle = document.getElementById('detail-title');
    const detailContent = document.getElementById('detail-content');
    const detailPane = document.getElementById('detail-pane');
    
    if (detailTitle && detailContent && detailPane && !document.querySelector('#mods-list tr.selected')) {
        detailPane.classList.add('hidden');
    }
    
    if (!overviewPane) return;
    
    if (state.mods.length === 0) {
        overviewPane.style.display = 'none';
        return;
    }
    
    overviewPane.style.display = 'flex';

    const updateMods = state.mods.filter(m => m.status === 'UpdateAvailable' && !m.isIgnored);
    const blockedMods = state.mods.filter(m => m.status === 'UpdateBlocked' && !m.isIgnored);
    const incompatMods = state.mods.filter(m => m.status === 'Incompatible' && !m.isIgnored);
    
    let summaryHtml = '';
    if (updateMods.length === 0 && blockedMods.length === 0 && incompatMods.length === 0) {
        summaryHtml = `<div style="flex: 1; background: var(--status-success-bg); border: 1px solid var(--status-success); color: var(--status-success); padding: 15px; border-radius: var(--radius-md);">
            <h3 style="margin-bottom: 10px;">All systems nominal</h3>
            <p>Your workspace is fully up to date with ${state.mods.length} mods installed.</p>
        </div>`;
    } else {
        summaryHtml = `<div style="flex: 1; background: var(--status-warning-bg); border: 1px solid var(--status-warning); color: var(--text-primary); padding: 15px; border-radius: var(--radius-md);">
            <h3 style="color: var(--status-warning); margin-bottom: 10px;">Action Required</h3>
            <p>Out of ${state.mods.length} total mods:</p>
            <ul style="margin-top:10px; margin-left: 20px;">
                ${updateMods.length > 0 ? `<li><strong>${updateMods.length}</strong> updates available</li>` : ''}
                ${blockedMods.length > 0 ? `<li><strong>${blockedMods.length}</strong> updates blocked</li>` : ''}
                ${incompatMods.length > 0 ? `<li><strong>${incompatMods.length}</strong> incompatible mods</li>` : ''}
            </ul>
        </div>`;
    }

    const bulkToolbar = `
        <div style="flex: 1; display: flex; flex-direction: column; justify-content: center;">
            <h4 style="color: var(--text-secondary); margin-bottom: 10px; text-transform: uppercase; font-size: 0.8rem;">Workspace Actions</h4>
            <div style="display: grid; grid-template-columns: 1fr; gap: 10px;">
                <button id="btn-copy-mods" class="btn-secondary">Copy Mods List to Clipboard</button>
                ${updateMods.filter(m => m.downloadUrl).length > 0 ? `<button id="btn-download-updates" class="btn-primary">Download Updates (${updateMods.filter(m => m.downloadUrl).length})</button>` : ''}
                ${updateMods.filter(m => m.modUrl).length > 0 ? `<button id="btn-open-pages" class="btn-secondary">Open Update Pages (${updateMods.filter(m => m.modUrl).length})</button>` : ''}
                <button id="btn-manage-ignored" class="btn-secondary">Manage Ignored Mods</button>
                <button id="btn-edit-settings" class="btn-secondary">Edit Settings</button>
            </div>
        </div>
    `;

    overviewPane.innerHTML = summaryHtml + bulkToolbar;

    const btnCopyMods = document.getElementById('btn-copy-mods');
    if (btnCopyMods) {
        btnCopyMods.addEventListener('click', () => {
            const list = state.mods.map(m => `- ${m.name} (v${m.localVersion || 'Unknown'}) - ${m.status}`).join('\n');
            navigator.clipboard.writeText(list).then(() => {
                const orig = btnCopyMods.textContent;
                btnCopyMods.textContent = 'Copied!';
                setTimeout(() => btnCopyMods.textContent = orig, 2000);
            });
        });
    }

    const btnDownloadUpdates = document.getElementById('btn-download-updates');
    if (btnDownloadUpdates) {
        btnDownloadUpdates.addEventListener('click', async () => {
            const dlMods = updateMods.filter(m => m.downloadUrl);
            for (const m of dlMods) {
                try {
                    await systemOpen(m.downloadUrl);
                } catch (e) {
                    logToConsole(`> Error opening download for ${m.name}: ${e}`, 'error');
                }
            }
        });
    }

    const btnOpenPages = document.getElementById('btn-open-pages');
    if (btnOpenPages) {
        btnOpenPages.addEventListener('click', async () => {
            const pageMods = updateMods.filter(m => m.modUrl);
            for (const m of pageMods) {
                try {
                    await systemOpen(m.modUrl);
                } catch (e) {
                    logToConsole(`> Error opening page for ${m.name}: ${e}`, 'error');
                }
            }
        });
    }

    const btnManageIgnored = document.getElementById('btn-manage-ignored');
    if (btnManageIgnored) {
        btnManageIgnored.addEventListener('click', async () => {
            await renderIgnoreDashboard();
            const modal = document.getElementById('ignore-modal');
            if (modal) modal.classList.remove('hidden');
        });
    }

    const btnEditSettings = document.getElementById('btn-edit-settings');
    if (btnEditSettings) {
        btnEditSettings.addEventListener('click', async () => {
            try {
                await systemOpen('appsettings.json');
            } catch (e) {
                logToConsole(`> Error opening settings: ${e}`, 'error');
            }
        });
    }
}
