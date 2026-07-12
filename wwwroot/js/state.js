export const state = {
    mods: [],
    filteredMods: [],
    filters: { search: '', status: 'all', type: 'all' },
    sort: { column: 'status', direction: 'asc' },
    ui: { consoleCollapsed: false, scanning: false, expandedRows: new Set(), selectedIds: new Set() },
    meta: { sptVersion: null, appVersion: null, lastScan: null, theme: 'dark' }
};

export function applyFilters(mods, filters) {
    return mods.filter(mod => {
        if (filters.search) {
            const query = filters.search;
            const matchName = mod.name && mod.name.toLowerCase().includes(query);
            const matchAuthor = mod.author && mod.author.toLowerCase().includes(query);
            if (!matchName && !matchAuthor) return false;
        }
        if (filters.status !== 'all') {
            const isUpToDate = mod.status === 'UpToDate' || mod.status === 'NewerInstalled';
            if (filters.status === 'ok' && !isUpToDate) return false;
            if (filters.status === 'attention' && isUpToDate) return false;
        }
        return true;
    });
}

export function applySort(mods, sort) {
    return [...mods].sort((a, b) => {
        let valA, valB;
        if (sort.column === 'name') {
            valA = a.name ? a.name.toLowerCase() : '';
            valB = b.name ? b.name.toLowerCase() : '';
        } else if (sort.column === 'status') {
            const order = { 'UpdateBlocked': 0, 'Incompatible': 1, 'Error': 2, 'NoVersionsFound': 3, 'UpdateAvailable': 4, 'NewerInstalled': 5, 'UpToDate': 6, 'Unknown': 7 };
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
        } else {
            valA = ''; valB = '';
        }
        
        if (valA < valB) return sort.direction === 'asc' ? -1 : 1;
        if (valA > valB) return sort.direction === 'asc' ? 1 : -1;
        return 0;
    });
}
