import { state, selectors, applyFilters, applySort , setMods, setFilteredMods, setSearchFilter, setStatusFilter, setSortColumn, setSortDirection, setScanning, setConsoleCollapsed, setLastFocus, setLastScan, setAppVersion, setSptVersion, setThemeMeta, clearSelectedIds, addSelectedId, removeSelectedId } from '../state.js';;
import { escapeHtml } from '../utils.js';
import { renderEmptyState } from './components.js';
import { renderVersionCell, renderStatusPill } from './renderers.js';
import { renderBulkBar, showOverview } from './dashboard.js';

/**
 * Renders the top health banner indicating overall mod workspace status.
 * @param {Array<Object>} mods - List of all mods.
 */
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

/**
 * Renders statistics (removed feature, handled in Workspace Overview).
 * @param {Array<Object>} mods - List of all mods.
 * @param {Object} filters - Current filters.
 */
export function renderStats(mods, filters) {
    // Top health board removed. Stats are now handled in Workspace Overview.
}

/**
 * Renders the main mods table body.
 * @param {Array<Object>} filteredMods - Filtered list of mods.
 * @param {Object} sort - Sort state.
 * @param {Object} ui - UI state.
 */
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
        else if (mod.status === 'UpdateAvailable') statusClass = 'status-update';
        else if (['UpdateBlocked', 'Incompatible', 'Error', 'NoVersionsFound'].includes(mod.status)) statusClass = 'status-blocked';

        const escapedName = escapeHtml(mod.name || 'Unknown');
        const escapedAuthor = escapeHtml(mod.author || 'Unknown');
        const duplicateHtml = mod.isDuplicate ? '<span class="status-badge badge badge-warning ml-2">DUPLICATE</span>' : '';
        const warningHtml = mod.hasWarnings ? '<span title="Mod has warnings. Check details pane." class="text-warning ml-1 text-sm">⚠️</span>' : '';
        let typeLabel = '';
        if (mod.isPaired) {
            typeLabel = '<span class="text-success font-semibold" title="Server Mod">Server</span> <span class="opacity-50 mx-1">&amp;</span> <span class="text-info font-semibold" title="Client Mod">Client</span>';
        } else if (mod.isServerMod) {
            typeLabel = '<span class="text-success font-semibold" title="Server Mod">Server</span>';
        } else {
            typeLabel = '<span class="text-info font-semibold" title="Client Mod">Client</span>';
        }
        
        const actionHtml = renderVersionCell(mod);
        const statusPill = renderStatusPill(mod.status, mod.isIgnored, mod.ignoreSource);

        tr.innerHTML = `
            <td>
                <input type="checkbox" class="row-checkbox action-select" value="${escapeHtml(mod.id)}" aria-label="Select ${escapedName}" ${ui.selectedIds.has(String(mod.id)) ? 'checked' : ''}>
            </td>
            <td data-label="Status">
                <div class="flex items-center gap-md">
                    <div class="status-dot ${statusClass}" title="${mod.status}"></div>
                    ${statusPill}
                </div>
            </td>
            <td data-label="Mod Name">
                <div class="mod-card-primary">
                    <div class="mod-card-title font-semibold">${escapedName}${warningHtml}${duplicateHtml}</div>
                    <div class="mod-card-meta">by ${escapedAuthor}</div>
                </div>
            </td>
            <td data-label="Type">
                ${typeLabel}
            </td>
            <td data-label="Version" class="col-version text-right">
                ${actionHtml}
            </td>
        `;
        
        modsList.appendChild(tr);
    });
}

/**
 * Updates the document title based on the number of outdated/actionable mods.
 * @param {Array<Object>} mods - List of all mods.
 */
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

/**
 * Updates the counts displayed on filter chips.
 * @param {Array<Object>} mods - List of all mods.
 * @param {Array<Object>} filteredMods - Filtered list of mods.
 * @param {Object} filters - Current filters.
 */
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

/**
 * Main render cycle. Applies filters, sorting, and triggers rendering of UI components.
 */
export function render() {
    setFilteredMods(applyFilters(state.mods, state.filters));
    setFilteredMods(applySort(state.filteredMods, state.sort));
    
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
    } else {
        const overviewPane = document.getElementById('workspace-overview');
        if (overviewPane) overviewPane.classList.add('hidden');
    }
}

/**
 * Sets the current status filter and triggers a re-render.
 * @param {string} filter - The filter to apply.
 */
export function setFilter(filter) {
    setStatusFilter(filter);
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

/**
 * Sets the UI theme and updates local storage.
 * @param {string} theme - 'dark' or 'light'.
 */
export function setTheme(theme) {
    setThemeMeta(theme);
    document.documentElement.dataset.theme = theme;
    localStorage.setItem('cme-theme', theme);
    const btnTheme = document.getElementById('btn-theme');
    if (btnTheme) btnTheme.textContent = theme === 'dark' ? '🌙' : '☀️';
}
