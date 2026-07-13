/**
 * Global application state object holding mods, filters, sort, UI state, and metadata.
 * @type {Object}
 */
export const state = {
    mods: [],
    filteredMods: [],
    filters: { search: '', status: 'all', type: 'all' },
    sort: { column: 'status', direction: 'asc' },
    ui: { consoleCollapsed: false, scanning: false, expandedRows: new Set(), selectedIds: new Set() },
    meta: { sptVersion: null, appVersion: null, lastScan: null, theme: 'dark' }
};

/**
 * Selector functions for deriving specific subsets of mods from the state.
 * @type {Object}
 */
export const selectors = {
    activeMods: (s) => s.mods.filter(m => !m.isIgnored),
    ignoredMods: (s) => s.mods.filter(m => m.isIgnored),
    updatesAvailable: (s) => selectors.activeMods(s).filter(m => m.status === 'UpdateAvailable'),
    updatesBlocked: (s) => selectors.activeMods(s).filter(m => m.status === 'UpdateBlocked'),
    incompatibleMods: (s) => selectors.activeMods(s).filter(m => m.status === 'Incompatible'),
    attentionRequired: (s) => selectors.activeMods(s).filter(m => ['UpdateAvailable', 'UpdateBlocked', 'Incompatible'].includes(m.status)),
    downloadableUpdates: (s) => selectors.updatesAvailable(s).filter(m => m.downloadUrl),
    pageUpdates: (s) => selectors.updatesAvailable(s).filter(m => m.modUrl),
    upToDate: (s) => selectors.activeMods(s).filter(m => ['UpToDate', 'NewerInstalled'].includes(m.status)),
};

/**
 * Applies current search and status filters to the list of mods.
 * @param {Array<Object>} mods - List of mod objects.
 * @param {Object} filters - Filter criteria.
 * @returns {Array<Object>} Filtered array of mods.
 */
export function applyFilters(mods, filters) {
    return mods.filter(mod => {
        if (filters.search) {
            const query = filters.search;
            const matchName = mod.name && mod.name.toLowerCase().includes(query);
            const matchAuthor = mod.author && mod.author.toLowerCase().includes(query);
            if (!matchName && !matchAuthor) return false;
        }
        
        if (filters.status !== 'all') {
            if (filters.status === 'ignored') {
                return mod.isIgnored === true;
            }
            if (mod.isIgnored) return false; // Hide ignored from other filters
            
            if (filters.status === 'ok') {
                return mod.status === 'UpToDate' || mod.status === 'NewerInstalled';
            }
            if (filters.status === 'attention') {
                return ['UpdateAvailable', 'UpdateBlocked', 'Incompatible'].includes(mod.status);
            }
        } else {
            // In 'all' view, you might want to show them, but maybe visual indicator
        }
        return true;
    });
}

/**
 * Sorts the list of mods based on the provided sort criteria.
 * Heuristics:
 * - name: Alphabetical string comparison.
 * - status: Custom severity order (e.g. UpdateAvailable first), with ignored mods typically at the bottom.
 * - version: Parsed semantic version comparison (e.g. 1.2.10 > 1.2.2).
 * - type: Orders by Client, Server, Paired based on the selected sort direction.
 *
 * @param {Array<Object>} mods - List of mod objects to sort.
 * @param {Object} sort - Sort criteria containing column and direction.
 * @returns {Array<Object>} Sorted array of mods.
 */
export function applySort(mods, sort) {
    return [...mods].sort((a, b) => {
        let valA, valB;
        if (sort.column === 'name') {
            valA = a.name ? a.name.toLowerCase() : '';
            valB = b.name ? b.name.toLowerCase() : '';
        } else if (sort.column === 'status') {
            if (a.isIgnored && !b.isIgnored) return sort.direction === 'asc' ? 1 : -1;
            if (!a.isIgnored && b.isIgnored) return sort.direction === 'asc' ? -1 : 1;

            const order = { 'UpdateAvailable': 0, 'UpdateBlocked': 1, 'Incompatible': 2, 'Error': 3, 'NoVersionsFound': 4, 'NewerInstalled': 5, 'UpToDate': 6, 'Unknown': 7 };
            valA = order[a.status] !== undefined ? order[a.status] : 8;
            valB = order[b.status] !== undefined ? order[b.status] : 8;
        } else if (sort.column === 'version') {
            const parseVer = v => (v || '').split('-')[0].split('.').map(n => parseInt(n) || 0);
            const pA = parseVer(a.localVersion);
            const pB = parseVer(b.localVersion);
            const len = Math.max(pA.length, pB.length);
            let cmp = 0;
            for (let i = 0; i < len; i++) {
                const nA = pA[i] || 0;
                const nB = pB[i] || 0;
                if (nA > nB) { cmp = 1; break; }
                if (nA < nB) { cmp = -1; break; }
            }
            return sort.direction === 'asc' ? cmp : -cmp;
        } else if (sort.column === 'type') {
            if (sort.direction === 'asc' || sort.direction === 'paired') {
                // 'asc' (lowest first): Client -> Server -> Paired
                // 'paired' (highest first): Paired -> Server -> Client
                const getTypeScore = m => m.isPaired ? 2 : (m.isServerMod ? 1 : 0);
                valA = getTypeScore(a);
                valB = getTypeScore(b);
            } else {
                // 'desc' (highest first): Server -> Client -> Paired
                const getTypeScore = m => m.isPaired ? 0 : (m.isServerMod ? 2 : 1);
                valA = getTypeScore(a);
                valB = getTypeScore(b);
            }
        } else {
            valA = ''; valB = '';
        }
        
        if (valA < valB) return sort.direction === 'asc' ? -1 : 1;
        if (valA > valB) return sort.direction === 'asc' ? 1 : -1;
        
        const nameA = a.name ? a.name.toLowerCase() : '';
        const nameB = b.name ? b.name.toLowerCase() : '';
        if (nameA < nameB) return -1;
        if (nameA > nameB) return 1;

        return 0;
    });
}

export function setMods(mods) { state.mods = mods; }
export function setFilteredMods(filteredMods) { state.filteredMods = filteredMods; }
export function setSearchFilter(search) { state.filters.search = search; }
export function setStatusFilter(status) { state.filters.status = status; }
export function setSort(column, direction) { state.sort.column = column; state.sort.direction = direction; }
export function setSortColumn(column) { state.sort.column = column; }
export function setSortDirection(direction) { state.sort.direction = direction; }
export function setScanning(scanning) { state.ui.scanning = scanning; }
export function setConsoleCollapsed(collapsed) { state.ui.consoleCollapsed = collapsed; }
export function setLastFocus(lastFocus) { state.ui.lastFocus = lastFocus; }
export function setLastScan(lastScan) { state.meta.lastScan = lastScan; }
export function setAppVersion(appVersion) { state.meta.appVersion = appVersion; }
export function setSptVersion(sptVersion) { state.meta.sptVersion = sptVersion; }
export function setThemeMeta(theme) { state.meta.theme = theme; }
export function clearSelectedIds() { state.ui.selectedIds.clear(); }
export function addSelectedId(id) { state.ui.selectedIds.add(String(id)); }
export function removeSelectedId(id) { state.ui.selectedIds.delete(String(id)); }
