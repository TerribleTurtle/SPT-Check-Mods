import { state, selectors } from '../state.js';
import { fetchIgnores, systemOpen } from '../api.js';
import { escapeHtml, logToConsole } from '../utils.js';

/**
 * Renders the bulk actions bar when multiple mods are selected.
 * @param {Set<string>} selectedIds - Set of selected mod IDs.
 */
export function renderBulkBar(selectedIds) {
    const bulkBar = document.getElementById('bulk-bar');
    const bulkCount = document.getElementById('bulk-count');
    if (!bulkBar || !bulkCount) return;

    if (selectedIds.size > 0) {
        bulkBar.classList.remove('bulk-bar-hidden');
        bulkCount.textContent = `${selectedIds.size} selected`;
    } else {
        bulkBar.classList.add('bulk-bar-hidden');
    }
}

/**
 * Fetches and renders the ignore dashboard modal content.
 * @returns {Promise<string>} HTML string for the modal body.
 */
export async function renderIgnoreDashboard() {
    try {
        const ignores = await fetchIgnores();
        
        let html = '';
        if (!ignores || ignores.length === 0) {
            html += '<p class="text-muted text-sm">No mods are currently ignored.</p>';
        } else {
            html += '<ul class="dashboard-list">';
            ignores.forEach(ig => {
                html += `<li class="dashboard-list-item">
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
            modalBody.innerHTML = `<div class="text-error mt-20">Error loading ignores: ${e.message}</div>`;
        }
    }
}

/**
 * Shows the workspace overview and builds the summary HTML logic based on the count
 * of updates available, updates blocked, and incompatible mods.
 * It injects a success message if all systems are nominal, or a warning list otherwise.
 *
 * @returns {Promise<void>}
 */
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

    const updateMods = selectors.updatesAvailable(state);
    const blockedMods = selectors.updatesBlocked(state);
    const incompatMods = selectors.incompatibleMods(state);
    const activeCount = selectors.activeMods(state).length;
    
    let summaryHtml = '';
    if (updateMods.length === 0 && blockedMods.length === 0 && incompatMods.length === 0) {
        summaryHtml = `<div class="dashboard-summary-box dashboard-summary-success">
            <h3 class="mb-2">All systems nominal</h3>
            <p>Your workspace is fully up to date with ${activeCount} active mods installed.</p>
        </div>`;
    } else {
        summaryHtml = `<div class="dashboard-summary-box dashboard-summary-warning">
            <h3 class="text-warning mb-2">Action Required</h3>
            <p>Out of ${activeCount} active mods:</p>
            <ul class="mt-2 ml-5">
                ${updateMods.length > 0 ? `<li><strong>${updateMods.length}</strong> updates available</li>` : ''}
                ${blockedMods.length > 0 ? `<li><strong>${blockedMods.length}</strong> updates blocked</li>` : ''}
                ${incompatMods.length > 0 ? `<li><strong>${incompatMods.length}</strong> incompatible mods</li>` : ''}
            </ul>
        </div>`;
    }

    const downloadableUpdates = selectors.downloadableUpdates(state);
    const pageUpdates = selectors.pageUpdates(state);

    const downloadCount = downloadableUpdates.length;
    const pageCount = pageUpdates.length;
    
    let downloadTooltip = '';
    if (downloadCount < pageCount) {
        downloadTooltip = `title="${pageCount - downloadCount} update(s) must be downloaded manually from their mod page"`;
    }

    const bulkToolbar = `
        <div class="flex flex-col justify-center flex-1">
            <h4 class="text-muted mb-2 uppercase text-sm">Workspace Actions</h4>
            <div class="grid grid-cols-1 gap-sm">
                <button id="btn-copy-mods" class="btn-secondary">Copy Mods List to Clipboard</button>
                <button id="btn-download-updates" class="btn-primary" ${downloadCount === 0 ? 'disabled' : ''} ${downloadTooltip}>
                    Download Updates ${downloadCount > 0 ? `(${downloadCount})` : ''}
                </button>
                <button id="btn-open-pages" class="btn-secondary" ${pageCount === 0 ? 'disabled' : ''}>
                    Open Update Pages ${pageCount > 0 ? `(${pageCount})` : ''}
                </button>
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
            for (const m of downloadableUpdates) {
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
            for (const m of pageUpdates) {
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
            state.ui.lastFocus = document.activeElement;
            await renderIgnoreDashboard();
            const modal = document.getElementById('ignore-modal');
            if (modal) {
                modal.classList.remove('hidden');
                setTimeout(() => {
                    const closeBtn = document.getElementById('btn-close-modal');
                    if (closeBtn) closeBtn.focus();
                }, 50);
            }
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
