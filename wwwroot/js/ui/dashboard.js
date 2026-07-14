import { state, selectors , setMods, setFilteredMods, setSearchFilter, setStatusFilter, setSortColumn, setSortDirection, setScanning, setConsoleCollapsed, setLastFocus, setLastScan, setAppVersion, setSptVersion, setThemeMeta, clearSelectedIds, addSelectedId, removeSelectedId } from '../state.js';;
import { fetchIgnores, systemOpen, fetchSettings, saveSettings } from '../api.js';
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
    const modalBody = document.getElementById('ignore-modal-body');
    if (modalBody) {
        modalBody.innerHTML = `<div class="flex justify-center p-20"><div class="loader-spinner spinner-sm"></div><span class="ml-2 text-muted">Loading ignored mods...</span></div>`;
    }
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
        overviewPane.classList.add('hidden');
        return;
    }
    
    overviewPane.classList.remove('hidden');

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
                    import('./components.js').then(c => c.showToast(`Error opening download for ${m.name}: ${e}`, 'error'));
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
                    import('./components.js').then(c => c.showToast(`Error opening page for ${m.name}: ${e}`, 'error'));
                }
            }
        });
    }

    const btnManageIgnored = document.getElementById('btn-manage-ignored');
    if (btnManageIgnored) {
        btnManageIgnored.addEventListener('click', async () => {
            setLastFocus(document.activeElement);
            const modal = document.getElementById('ignore-modal');
            if (modal) {
                modal.classList.remove('hidden');
                setTimeout(() => {
                    const closeBtn = document.getElementById('btn-close-modal');
                    if (closeBtn) closeBtn.focus();
                }, 50);
            }
            await renderIgnoreDashboard();
        });
    }

    const btnEditSettings = document.getElementById('btn-edit-settings');
    if (btnEditSettings) {
        btnEditSettings.addEventListener('click', async () => {
            setLastFocus(document.activeElement);
            const modal = document.getElementById('settings-modal');
            if (modal) {
                modal.classList.remove('hidden');
                setTimeout(() => {
                    const closeBtn = document.getElementById('btn-close-settings-modal');
                    if (closeBtn) closeBtn.focus();
                }, 50);
            }
            await renderSettingsDashboard();
        });
    }
}

/**
 * Renders the settings dashboard modal content.
 */
export async function renderSettingsDashboard() {
    const modalBody = document.getElementById('settings-modal-body');
    if (modalBody) {
        modalBody.innerHTML = `<div class="flex justify-center p-20"><div class="loader-spinner spinner-sm"></div><span class="ml-2 text-muted">Loading settings...</span></div>`;
    }
    try {
        const settings = await fetchSettings();
        
        let html = '<div class="settings-form grid grid-cols-1 gap-md">';
        
        // AppPaths
        html += `<div class="settings-group">
            <h3 class="mb-2">App Paths</h3>
            <label class="form-label">App Data Directory</label>
            <div class="text-muted text-sm mb-2">The root directory for logs, caches, and application state. Default: <code>%APPDATA%\\SptCheckModsExtended</code></div>
            <input type="text" id="setting-AppDataDirectory" class="form-input w-full" value="${escapeHtml(settings.AppPaths?.AppDataDirectory || '')}" placeholder="Leave empty for default">
        </div>`;
        
        // LoggingOptions
        html += `<div class="settings-group">
            <h3 class="mb-2">Logging Options</h3>
            <label class="form-label flex align-center gap-sm">
                <input type="checkbox" id="setting-EnableFileLogging" class="row-checkbox" ${settings.LoggingOptions?.EnableFileLogging ? 'checked' : ''}> Enable File Logging
            </label>
            <div class="text-muted text-sm mb-4">Write application logs to disk for troubleshooting.</div>

            <label class="form-label">Minimum Log Level</label>
            <div class="text-muted text-sm mb-2">Controls the verbosity of the log file.</div>
            <select id="setting-MinimumLogLevel" class="form-input w-full">
                <option value="Debug" ${settings.LoggingOptions?.MinimumLogLevel === 'Debug' ? 'selected' : ''}>Debug</option>
                <option value="Information" ${settings.LoggingOptions?.MinimumLogLevel === 'Information' ? 'selected' : ''}>Information</option>
                <option value="Warning" ${settings.LoggingOptions?.MinimumLogLevel === 'Warning' ? 'selected' : ''}>Warning</option>
                <option value="Error" ${settings.LoggingOptions?.MinimumLogLevel === 'Error' ? 'selected' : ''}>Error</option>
            </select>

            <label class="form-label mt-4">Log File Path</label>
            <div class="text-muted text-sm mb-2">The filename for the log file (relative to AppDataDirectory/logs). Default: <code>checkmod.log</code></div>
            <input type="text" id="setting-LogFilePath" class="form-input w-full" value="${escapeHtml(settings.LoggingOptions?.LogFilePath || '')}">
            
            <label class="form-label mt-4">Max File Size (Bytes)</label>
            <div class="text-muted text-sm mb-2">Maximum size of a single log file before it rolls over. Default: 10485760 (10MB)</div>
            <input type="number" id="setting-MaxFileSizeBytes" class="form-input w-full" value="${settings.LoggingOptions?.MaxFileSizeBytes || 10485760}">
        </div>`;

        html += '</div>';
        
        if (modalBody) {
            modalBody.innerHTML = html;
        }

        const btnSave = document.getElementById('btn-save-settings');
        if (btnSave) {
            btnSave.onclick = async () => {
                const updatedSettings = {
                    ...settings,
                    AppPaths: {
                        ...settings.AppPaths,
                        AppDataDirectory: document.getElementById('setting-AppDataDirectory').value
                    },
                    LoggingOptions: {
                        ...settings.LoggingOptions,
                        EnableFileLogging: document.getElementById('setting-EnableFileLogging').checked,
                        MinimumLogLevel: document.getElementById('setting-MinimumLogLevel').value,
                        LogFilePath: document.getElementById('setting-LogFilePath').value,
                        MaxFileSizeBytes: parseInt(document.getElementById('setting-MaxFileSizeBytes').value, 10)
                    }
                };
                
                try {
                    await saveSettings(updatedSettings);
                    document.getElementById('settings-modal').classList.add('hidden');
                    import('./components.js').then(c => c.showToast('Settings saved! A restart may be required.', 'success'));
                } catch (e) {
                    import('./components.js').then(c => c.showToast(`Error saving settings: ${e.message}`, 'error'));
                }
            };
        }
        
    } catch (e) {
        if (modalBody) {
            modalBody.innerHTML = `<div class="text-error mt-20">Error loading settings: ${e.message}</div>`;
        }
    }
}
