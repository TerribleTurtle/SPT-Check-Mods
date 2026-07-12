import { state, applyFilters, applySort } from './state.js';
import { escapeHtml, logToConsole } from './utils.js';
import { fetchIgnores, systemOpen } from './api.js';

export function renderHealthBanner(mods) {
    const healthBanner = document.getElementById('health-banner');
    if (!healthBanner) return;
    
    if (mods.length === 0) {
        healthBanner.hidden = true;
        return;
    }

    const upToDateMods = mods.filter(m => m.status === 'UpToDate' || m.status === 'NewerInstalled');
    const actionableMods = mods.length - upToDateMods.length;

    healthBanner.hidden = false;
    healthBanner.className = 'health-banner'; 

    if (actionableMods === 0) {
        healthBanner.classList.add('health-banner-ok');
        healthBanner.textContent = `All ${mods.length} mods up to date ✓`;
    } else {
        healthBanner.classList.add('health-banner-warn');
        healthBanner.textContent = `${actionableMods} of ${mods.length} mods need attention ⚠`;
    }
}

export function renderStats(mods, filters) {
    const healthBoard = document.getElementById('health-board');
    if (!healthBoard) return;
    
    if (mods.length === 0) {
        healthBoard.style.display = 'none';
        return;
    }

    const totalMods = mods.length;
    const upToDateMods = mods.filter(m => m.status === 'UpToDate' || m.status === 'NewerInstalled');
    const actionableMods = mods.length - upToDateMods.length;
    
    const elTotal = document.getElementById('stat-total');
    if (elTotal) elTotal.textContent = totalMods;
    
    const elOutdated = document.getElementById('stat-outdated');
    if (elOutdated) elOutdated.textContent = actionableMods;
    
    const elOk = document.getElementById('stat-ok');
    if (elOk) elOk.textContent = upToDateMods.length;
    
    healthBoard.style.display = window.innerWidth > 768 ? 'grid' : 'flex';
    if (window.innerWidth <= 768) {
        healthBoard.style.flexDirection = 'column';
    }
}

export function renderStatusPill(status) {
    let text, colorClass;
    switch(status) {
        case 'UpToDate': text = 'UP TO DATE'; colorClass = 'status-pill-ok'; break;
        case 'UpdateAvailable': text = 'UPDATE'; colorClass = 'status-pill-update'; break;
        case 'UpdateBlocked': text = 'BLOCKED'; colorClass = 'status-pill-blocked'; break;
        case 'Incompatible': text = 'INCOMPATIBLE'; colorClass = 'status-pill-incompat'; break;
        case 'NewerInstalled': text = 'NEWER'; colorClass = 'status-pill-newer'; break;
        default: text = 'UNKNOWN'; colorClass = 'status-pill-unknown'; break;
    }
    return `<span class="status-pill ${colorClass}" title="Status: ${text}">${text}</span>`;
}

export function renderVersionCell(mod) {
    if (mod.status === 'UpdateAvailable') {
        return `<span class="version-outdated" title="Current Version">v${escapeHtml(mod.localVersion)}</span> <span class="version-arrow">→</span> <span class="version-newer" title="Latest Version">v${escapeHtml(mod.latestVersion)}</span>`;
    }
    if (mod.localVersion) {
        return `<span class="version-match" title="Current Version">v${escapeHtml(mod.localVersion)}</span>`;
    }
    return '';
}

export function renderActions(mod) {
    let actions = '';
    if (mod.downloadUrl) {
        actions += `<button class="btn-primary action-system-open" data-target="${escapeHtml(mod.downloadUrl)}">Download ZIP</button> `;
    }
    if (mod.modUrl) {
        actions += `<button class="btn-secondary action-system-open" data-target="${escapeHtml(mod.modUrl)}">Open Mod Page</button> `;
    }
    if (mod.localDirectory) {
        actions += `<button class="btn-secondary action-system-open" data-target="${escapeHtml(mod.localDirectory)}">Open Local Folder</button> `;
    }
    if (mod.status === 'UpdateAvailable') {
        actions += `<button class="btn-secondary action-ignore" data-id="${escapeHtml(mod.id)}" data-local="${escapeHtml(mod.localVersion)}" data-latest="${escapeHtml(mod.latestVersion)}">Ignore Update</button> `;
    }
    if (mod.isIgnored) {
         actions += `<button class="btn-secondary action-unignore" data-id="${escapeHtml(mod.id)}">Un-Ignore</button> `;
    }
    return actions;
}

export function renderDetailRow(mod) {
    const detailTitle = document.getElementById('detail-title');
    const detailContent = document.getElementById('detail-content');
    const detailPane = document.getElementById('detail-pane');
    
    detailTitle.textContent = mod.name || 'Unknown Mod';
    let html = '';
    
    if (mod.status === 'UpdateBlocked') {
        html += `<div style="background: var(--status-error-bg); border: 1px solid var(--status-error); padding: 15px; border-radius: var(--radius-md); margin-bottom: 20px;">
            <h3 style="color: var(--status-error); margin-bottom: 10px;">Update Blocked</h3>
            <p>${escapeHtml(mod.blockReason ? mod.blockReason.replace(/_/g, ' ') : 'Unknown reason')}</p>
        </div>`;
    } else if (mod.status === 'Incompatible') {
        html += `<div style="background: var(--status-error-bg); border: 1px solid var(--status-error); padding: 15px; border-radius: var(--radius-md); margin-bottom: 20px;">
            <h3 style="color: var(--status-error); margin-bottom: 10px;">Incompatible with SPT</h3>
            <p>${escapeHtml(mod.incompatibilityReason || 'Unknown')}</p>
            ${mod.compatibleVersion ? `<p style="margin-top: 10px;">Latest compatible version: <strong>${escapeHtml(mod.compatibleVersion)}</strong></p>` : ''}
        </div>`;
    } else if (mod.status === 'UpdateAvailable' || mod.isIgnored) {
        html += `<div style="background: var(--status-warning-bg); border: 1px solid var(--status-warning); padding: 15px; border-radius: var(--radius-md); margin-bottom: 20px;">
            <h3 style="color: var(--status-warning); margin-bottom: 10px;">${mod.isIgnored ? 'Update Ignored' : 'Update Available'}</h3>
            <p>Version <strong class="ver-ok">${escapeHtml(mod.latestVersion)}</strong> is available. You are running <strong class="ver-warn">${escapeHtml(mod.localVersion)}</strong>.</p>
            <div style="margin-top: 15px; display: flex; gap: 10px;">
                ${renderActions(mod)}
            </div>
        </div>`;
    }

    let generalActions = '';
    if (mod.status !== 'UpdateAvailable' && !mod.isIgnored) {
        if (mod.modUrl) {
            generalActions += `<button class="btn-secondary action-system-open" data-target="${escapeHtml(mod.modUrl)}">Open Mod Page</button> `;
        }
        if (mod.localDirectory) {
            generalActions += `<button class="btn-secondary action-system-open" data-target="${escapeHtml(mod.localDirectory)}">Open Local Folder</button> `;
        }
    }
    if (generalActions) {
        html += `<div style="margin-bottom: 20px; display: flex; gap: 10px;">
            ${generalActions}
        </div>`;
    }
    
    let localVerClass = 'ver-ok';
    let latestVerClass = 'ver-ok';
    if (mod.status === 'UpdateAvailable' || mod.isIgnored) {
        localVerClass = 'ver-warn';
    } else if (mod.status === 'UpdateBlocked' || mod.status === 'Incompatible') {
        localVerClass = 'ver-error';
    } else if (mod.status === 'NewerInstalled') {
        localVerClass = 'ver-info';
    }

    html += `<div style="margin-bottom: 20px;">
        <h4 style="color: var(--text-secondary); margin-bottom: 8px; text-transform: uppercase; font-size: 0.8rem;">Metadata</h4>
        <div style="display: grid; grid-template-columns: 100px 1fr; gap: 8px; font-size: 0.9rem;">
            <span style="color: var(--text-muted);">Author</span>
            <span>${escapeHtml(mod.author || 'Unknown')}</span>
            <span style="color: var(--text-muted);">Local Ver</span>
            <span class="${localVerClass}">${escapeHtml(mod.localVersion || 'N/A')}</span>
            <span style="color: var(--text-muted);">Latest Ver</span>
            <span class="${latestVerClass}">${escapeHtml(mod.latestVersion || 'N/A')}</span>
            <span style="color: var(--text-muted);">Type</span>
            <span title="${mod.isServerMod ? 'Server Mod' : 'Client Mod'}">${mod.isServerMod ? 'Server Mod' : 'Client Mod'}</span>
        </div>
    </div>`;
    
    if (mod.addedDependencies && mod.addedDependencies.length > 0) {
        html += `<div style="margin-bottom: 20px;">
            <h4 style="color: var(--text-secondary); margin-bottom: 8px; text-transform: uppercase; font-size: 0.8rem;">Required Dependencies</h4>
            <div style="display: flex; flex-direction: column; gap: 8px;">`;
        
        mod.addedDependencies.forEach(dep => {
            let badge = '';
            if (dep.installState === 'NotInstalled') badge = `<span class="badge badge-error" title="Missing Dependency">MISSING</span>`;
            else if (dep.installState === 'InstalledOutdated') badge = `<span class="badge badge-warn" title="Outdated Dependency">OUTDATED</span>`;
            else badge = `<span class="badge badge-neutral" title="Satisfied Dependency">SATISFIED</span>`;
            
            let link = dep.downloadLink ? `<a href="${escapeHtml(dep.downloadLink)}" target="_blank" class="dep-link">${escapeHtml(dep.name)}</a>` : escapeHtml(dep.name);
            
            html += `<div style="background: var(--bg-elevated); padding: 10px; border-radius: var(--radius-sm); border: 1px solid var(--border-default);">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <span>${link}</span>
                    ${badge}
                </div>
                <div style="font-size: 0.8rem; color: var(--text-muted); margin-top: 4px;">Requires v${escapeHtml(dep.recommendedVersion)}</div>
            </div>`;
        });
        html += `</div></div>`;
    }

    if (mod.blockingMods && mod.blockingMods.length > 0) {
         html += `<div style="margin-bottom: 20px;">
            <h4 style="color: var(--text-secondary); margin-bottom: 8px; text-transform: uppercase; font-size: 0.8rem;">Blocked By</h4>
            <ul style="padding-left: 20px; color: var(--text-primary); font-size: 0.9rem;">`;
         mod.blockingMods.forEach(b => {
             html += `<li>${escapeHtml(b.name)} (constraint: ${escapeHtml(b.constraint)})</li>`;
         });
         html += `</ul></div>`;
    }
    
    detailContent.innerHTML = html;
    detailPane.classList.remove('hidden');
}

export function renderEmptyState(message, type = 'info', isFilterEmpty = false) {
    const modsList = document.getElementById('mods-list');
    if (!modsList) return;
    modsList.innerHTML = `
        <tr>
            <td colspan="4">
                <div class="empty-state" style="color: ${type === 'error' ? 'var(--status-error)' : 'inherit'};">
                    ${escapeHtml(message)}
                    ${isFilterEmpty ? '<br><br><button class="btn-secondary" id="btn-clear-filters">Clear Filters</button>' : ''}
                </div>
            </td>
        </tr>
    `;
    if (isFilterEmpty) {
        const btnClear = document.getElementById('btn-clear-filters');
        if (btnClear) {
            btnClear.addEventListener('click', () => {
                const searchInput = document.getElementById('search-input');
                if (searchInput) searchInput.value = '';
                state.filters.search = '';
                setFilter('all');
            });
        }
    }
}

export function renderTable(filteredMods, sort, ui) {
    const modsList = document.getElementById('mods-list');
    if (!modsList) return;

    if (!filteredMods || filteredMods.length === 0) {
        renderEmptyState(state.mods.length > 0 ? "No mods match your current filters." : "No mods detected in target directory.", 'info', state.mods.length > 0);
        return;
    }

    modsList.innerHTML = '';
    
    filteredMods.forEach(mod => {
        const tr = document.createElement('tr');
        if (ui.selectedIds.has(String(mod.id))) tr.classList.add('selected');
        tr.dataset.id = mod.id;
        
        let statusClass = 'status-unknown';
        if (mod.status === 'UpToDate') statusClass = 'status-ok';
        else if (mod.status === 'NewerInstalled') statusClass = 'status-newer';
        else if (mod.status === 'UpdateAvailable') statusClass = 'status-warn';
        else if (['UpdateBlocked', 'Incompatible', 'Error', 'NoVersionsFound'].includes(mod.status)) statusClass = 'status-error';

        const escapedName = escapeHtml(mod.name || 'Unknown');
        const escapedAuthor = escapeHtml(mod.author || 'Unknown');
        const typeLabel = mod.isServerMod 
            ? '<span style="color: var(--status-success); font-weight: 600;" title="Server Mod">Server</span>' 
            : '<span style="color: var(--status-info); font-weight: 600;" title="Client Mod">Client</span>';
        
        const actionHtml = renderVersionCell(mod);
        const statusPill = renderStatusPill(mod.status);

        tr.innerHTML = `
            <td>
                <input type="checkbox" class="row-checkbox action-select" value="${escapeHtml(mod.id)}" aria-label="Select mod" ${ui.selectedIds.has(String(mod.id)) ? 'checked' : ''}>
            </td>
            <td data-label="Status">
                <div style="display:flex; align-items:center; gap:var(--space-md);">
                    <div class="status-block ${statusClass}" title="${mod.status}" style="border-radius: 50%; box-shadow: 0 0 5px var(--status-${statusClass.split('-')[1]}); width: 12px; height: 12px; min-width: 12px;"></div>
                    ${statusPill}
                </div>
            </td>
            <td data-label="Mod Name">
                <div class="mod-card-primary">
                    <div class="mod-card-title" style="font-weight: 600;">${escapedName}</div>
                    <div class="mod-card-meta">by ${escapedAuthor} • ${typeLabel}</div>
                </div>
            </td>
            <td data-label="Version" class="col-version" style="text-align: right;">
                ${actionHtml}
            </td>
        `;
        
        modsList.appendChild(tr);
    });
}

export function updateTitle(mods) {
    if (!mods || mods.length === 0) {
        document.title = "CheckModsExtended // MANAGER";
        return;
    }
    const outdatedCount = mods.filter(m => m.status === 'UpdateAvailable' || m.status === 'UpdateBlocked').length;
    if (outdatedCount > 0) {
        document.title = `(${outdatedCount} outdated) Check Mods Extended`;
    } else {
        document.title = "CheckModsExtended // MANAGER";
    }
}

export function renderChipCounts(mods, filteredMods, filters) {
    const countAll = document.getElementById('chip-count-all');
    const countOk = document.getElementById('chip-count-ok');
    const countAttention = document.getElementById('chip-count-attention');
    const elSearchCount = document.getElementById('search-count');

    if (countAll) countAll.textContent = mods.length;
    if (countOk) countOk.textContent = mods.filter(m => m.status === 'UpToDate' || m.status === 'NewerInstalled').length;
    if (countAttention) countAttention.textContent = mods.filter(m => !(m.status === 'UpToDate' || m.status === 'NewerInstalled')).length;

    if (elSearchCount) {
        if (filters.search || filters.status !== 'all') {
            elSearchCount.textContent = `Showing ${filteredMods.length} of ${mods.length}`;
        } else {
            elSearchCount.textContent = '';
        }
    }
    
    document.querySelectorAll('th[data-sortable]').forEach(th => {
        if (th.dataset.sortable === state.sort.column) {
            th.setAttribute('aria-sort', state.sort.direction === 'asc' ? 'ascending' : 'descending');
        } else {
            th.setAttribute('aria-sort', 'none');
        }
    });
}

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
        
        let html = '<div class="ignore-dashboard" style="margin-top: 20px;">';
        html += '<h3 style="color: var(--text-primary); margin-bottom: 10px; font-size: 1.1rem;">Manage Ignore List</h3>';
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
        html += '</div>';
        
        return html;
    } catch (e) {
        return `<div style="color: var(--status-error); margin-top: 20px;">Error loading ignores: ${e.message}</div>`;
    }
}

export async function showOverview() {
    const detailTitle = document.getElementById('detail-title');
    const detailContent = document.getElementById('detail-content');
    const detailPane = document.getElementById('detail-pane');
    
    detailTitle.textContent = 'Workspace Overview';
    
    if (state.mods.length === 0) {
        detailContent.innerHTML = '<div class="empty-state">No mods loaded. Run a scan to populate the dashboard.</div>';
        detailPane.classList.remove('hidden');
        return;
    }

    const updateMods = state.mods.filter(m => m.status === 'UpdateAvailable');
    const blockedMods = state.mods.filter(m => m.status === 'UpdateBlocked');
    const incompatMods = state.mods.filter(m => m.status === 'Incompatible');
    
    let summaryHtml = '';
    if (updateMods.length === 0 && blockedMods.length === 0 && incompatMods.length === 0) {
        summaryHtml = `<div style="background: var(--status-success-bg); border: 1px solid var(--status-success); color: var(--status-success); padding: 15px; border-radius: var(--radius-md); margin-bottom: 20px;">
            <h3 style="margin-bottom: 10px;">All systems nominal</h3>
            <p>Your workspace is fully up to date with ${state.mods.length} mods installed.</p>
        </div>`;
    } else {
        summaryHtml = `<div style="background: var(--status-warning-bg); border: 1px solid var(--status-warning); color: var(--text-primary); padding: 15px; border-radius: var(--radius-md); margin-bottom: 20px;">
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
        <div style="margin-bottom: 20px;">
            <h4 style="color: var(--text-secondary); margin-bottom: 10px; text-transform: uppercase; font-size: 0.8rem;">Workspace Actions</h4>
            <div style="display: grid; grid-template-columns: 1fr; gap: 10px;">
                <button id="btn-copy-mods" class="btn-secondary">Copy Mods List to Clipboard</button>
                ${updateMods.filter(m => m.downloadUrl).length > 0 ? `<button id="btn-download-updates" class="btn-primary">Download Updates (${updateMods.filter(m => m.downloadUrl).length})</button>` : ''}
                ${updateMods.filter(m => m.modUrl).length > 0 ? `<button id="btn-open-pages" class="btn-secondary">Open Update Pages (${updateMods.filter(m => m.modUrl).length})</button>` : ''}
                <button id="btn-edit-settings" class="btn-secondary">Edit Settings</button>
            </div>
        </div>
    `;

    detailContent.innerHTML = summaryHtml + bulkToolbar;
    
    const ignoreHtml = await renderIgnoreDashboard();
    detailContent.innerHTML += ignoreHtml;
    
    detailPane.classList.remove('hidden');

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

export function render() {
    state.filteredMods = applyFilters(state.mods, state.filters);
    state.filteredMods = applySort(state.filteredMods, state.sort);
    
    renderHealthBanner(state.mods);
    renderStats(state.mods, state.filters);
    renderChipCounts(state.mods, state.filteredMods, state.filters);
    renderTable(state.filteredMods, state.sort, state.ui);
    renderBulkBar(state.ui.selectedIds);
    updateTitle(state.mods);

    // Sync select-all checkbox
    const selectAll = document.getElementById('select-all');
    if (selectAll) {
        selectAll.checked = state.filteredMods.length > 0 && state.filteredMods.every(m => state.ui.selectedIds.has(String(m.id)));
        selectAll.indeterminate = state.filteredMods.some(m => state.ui.selectedIds.has(String(m.id))) && !selectAll.checked;
    }

    // Show overview if no mod is selected
    if (!document.querySelector('#mods-list tr.selected')) {
        showOverview();
    }
}

export function setFilter(filter) {
    state.filters.status = filter;
    localStorage.setItem('cme-filter-status', filter);
    document.querySelectorAll('.chip').forEach(c => {
        if (c.dataset.filter === filter) c.classList.add('active');
        else c.classList.remove('active');
    });
    render();
}

export function setTheme(theme) {
    state.meta.theme = theme;
    document.documentElement.dataset.theme = theme;
    localStorage.setItem('cme-theme', theme);
    const btnTheme = document.getElementById('btn-theme');
    if (btnTheme) btnTheme.textContent = theme === 'dark' ? '🌙' : '☀️';
}

export function toggleConsole(collapsed) {
    state.ui.consoleCollapsed = collapsed;
    localStorage.setItem('cme-console-collapsed', collapsed);
    const consoleDrawer = document.getElementById('console-drawer');
    const btnConsoleToggle = document.getElementById('btn-console-toggle');
    if (collapsed) {
        consoleDrawer.classList.add('collapsed');
        btnConsoleToggle.textContent = '▲';
        btnConsoleToggle.setAttribute('aria-expanded', 'false');
    } else {
        consoleDrawer.classList.remove('collapsed');
        btnConsoleToggle.textContent = '▼';
        btnConsoleToggle.setAttribute('aria-expanded', 'true');
    }
}

export function handleCopyLog() {
    const consoleLogs = document.getElementById('console-logs');
    const btnCopyLog = document.getElementById('btn-copy-log');
    const text = Array.from(consoleLogs.querySelectorAll('.log-line'))
        .map(el => el.textContent)
        .join('\n');
    navigator.clipboard.writeText(text)
        .then(() => {
            const originalText = btnCopyLog.textContent;
            btnCopyLog.textContent = 'COPIED!';
            setTimeout(() => btnCopyLog.textContent = originalText, 2000);
        })
        .catch(err => logToConsole(`Failed to copy logs: ${err}`, 'error'));
}

export function updateLastScanTime() {
    if (!state.meta.lastScan) return;
    const lastScanEl = document.getElementById('last-scan-time');
    if (!lastScanEl) return;
    
    const seconds = Math.floor((Date.now() - state.meta.lastScan) / 1000);
    let text = 'Just now';
    if (seconds > 59) {
        const minutes = Math.floor(seconds / 60);
        text = `${minutes}m ago`;
    } else if (seconds > 10) {
        text = `${seconds}s ago`;
    }
    lastScanEl.textContent = `Last scanned: ${text}`;
}

let loaderInterval;
export function startLoaderAnimation() {
    const states = [
        '[ CONNECTING TO FORGE API... ]',
        '[ RECONCILING COMPONENT HASHES... ]',
        '[ ANALYZING MOD VERSIONS... ]',
        '[ CHECKING DEPENDENCIES... ]'
    ];
    let i = 0;
    const loaderText = document.getElementById('loader-text');
    if (!loaderText) return;
    
    loaderInterval = setInterval(() => {
        i = (i + 1) % states.length;
        const lt = document.getElementById('loader-text');
        if (lt) lt.textContent = states[i];
    }, 2500);
}

export function stopLoaderAnimation() {
    clearInterval(loaderInterval);
}
