import { state, selectors, applyFilters, applySort } from '../state.js';
import { escapeHtml } from '../utils.js';
import { renderEmptyState } from './components.js';
import { renderVersionCell, renderStatusPill } from './renderers.js';
import { renderBulkBar, showOverview } from './dashboard.js';

export function renderHealthBanner(mods) {
    const healthBanner = document.getElementById('health-banner');
    if (!healthBanner) return;
    
    if (mods.length === 0) {
        healthBanner.hidden = true;
        return;
    }

    const activeMods = selectors.activeMods(state);
    const actionableMods = selectors.attentionRequired(state).length;

    healthBanner.hidden = false;
    healthBanner.className = 'health-banner'; 

    if (actionableMods === 0) {
        healthBanner.classList.add('health-banner-ok');
        healthBanner.textContent = `All ${activeMods.length} active mods up to date ✓`;
    } else {
        healthBanner.classList.add('health-banner-warn');
        healthBanner.textContent = `${actionableMods} of ${activeMods.length} active mods need attention ⚠`;
    }
}

export function renderStats(mods, filters) {
    // Top health board removed. Stats are now handled in Workspace Overview.
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
        if (mod.isIgnored) statusClass = 'status-unknown';
        else if (mod.status === 'UpToDate') statusClass = 'status-ok';
        else if (mod.status === 'NewerInstalled') statusClass = 'status-newer';
        else if (mod.status === 'UpdateAvailable') statusClass = 'status-warn';
        else if (['UpdateBlocked', 'Incompatible', 'Error', 'NoVersionsFound'].includes(mod.status)) statusClass = 'status-error';

        const escapedName = escapeHtml(mod.name || 'Unknown');
        const escapedAuthor = escapeHtml(mod.author || 'Unknown');
        const duplicateHtml = mod.isDuplicate ? '<span class="status-badge" style="background-color: var(--status-warning); color: var(--text-dark); margin-left: 8px; font-size: 0.7rem; padding: 2px 6px; border-radius: 4px; font-weight: bold; vertical-align: middle;">DUPLICATE</span>' : '';
        const warningHtml = mod.hasWarnings ? '<span title="Mod has warnings. Check details pane." style="color: var(--status-warning); margin-left: 5px; font-size: 0.9rem;">⚠️</span>' : '';
        let typeLabel = '';
        if (mod.isPaired) {
            typeLabel = '<span style="color: var(--status-success); font-weight: 600;" title="Server Mod">Server</span> <span style="opacity:0.5; margin:0 2px;">&amp;</span> <span style="color: var(--status-info); font-weight: 600;" title="Client Mod">Client</span>';
        } else if (mod.isServerMod) {
            typeLabel = '<span style="color: var(--status-success); font-weight: 600;" title="Server Mod">Server</span>';
        } else {
            typeLabel = '<span style="color: var(--status-info); font-weight: 600;" title="Client Mod">Client</span>';
        }
        
        const actionHtml = renderVersionCell(mod);
        const statusPill = renderStatusPill(mod.status, mod.isIgnored);

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
                    <div class="mod-card-title" style="font-weight: 600;">${escapedName}${warningHtml}${duplicateHtml}</div>
                    <div class="mod-card-meta">by ${escapedAuthor}</div>
                </div>
            </td>
            <td data-label="Type">
                ${typeLabel}
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
    const outdatedCount = selectors.updatesAvailable(state).length + selectors.updatesBlocked(state).length;
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
    const countIgnored = document.getElementById('chip-count-ignored');
    const elSearchCount = document.getElementById('search-count');

    const ignoredMods = selectors.ignoredMods(state);
    const upToDateMods = selectors.upToDate(state);
    const attentionMods = selectors.attentionRequired(state);

    if (countAll) countAll.textContent = mods.length;
    if (countOk) countOk.textContent = upToDateMods.length;
    if (countAttention) countAttention.textContent = attentionMods.length;
    if (countIgnored) countIgnored.textContent = ignoredMods.length;

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

export function render() {
    state.filteredMods = applyFilters(state.mods, state.filters);
    state.filteredMods = applySort(state.filteredMods, state.sort);
    
    renderHealthBanner(state.mods);
    renderStats(state.mods, state.filters);
    renderChipCounts(state.mods, state.filteredMods, state.filters);
    renderTable(state.filteredMods, state.sort, state.ui);
    renderBulkBar(state.ui.selectedIds);
    updateTitle(state.mods);
    
    // Announce to screen readers
    const announcer = document.getElementById('a11y-announcer');
    if (announcer) {
        announcer.textContent = `Showing ${state.filteredMods.length} of ${state.mods.length} mods.`;
    }

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
        if (c.dataset.filter === filter) {
            c.classList.add('active');
            c.setAttribute('aria-pressed', 'true');
        } else {
            c.classList.remove('active');
            c.setAttribute('aria-pressed', 'false');
        }
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
