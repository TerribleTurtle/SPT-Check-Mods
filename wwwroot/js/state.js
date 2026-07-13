export const state = {
    mods: [],
    filteredMods: [],
    filters: { search: '', status: 'all', type: 'all' },
    sort: { column: 'status', direction: 'asc' },
    ui: { consoleCollapsed: false, scanning: false, expandedRows: new Set(), selectedIds: new Set() },
    meta: { sptVersion: null, appVersion: null, lastScan: null, theme: 'dark' }
};

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
            const getTypeScore = m => m.isPaired ? 2 : (m.isServerMod ? 0 : 1);
            valA = getTypeScore(a);
            valB = getTypeScore(b);
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
