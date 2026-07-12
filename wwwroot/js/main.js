import { state } from './state.js';
import { fetchStatus, fetchScan, ignoreMod, unignoreMod, systemOpen } from './api.js';
import { logToConsole } from './utils.js';
import { setTheme, setFilter, render } from './ui/table.js';
import { toggleConsole, handleCopyLog, updateLastScanTime, startLoaderAnimation, stopLoaderAnimation, renderEmptyState } from './ui/components.js';
import { showOverview } from './ui/dashboard.js';
import { renderDetailRow } from './ui/details.js';

document.addEventListener('DOMContentLoaded', () => {
    const btnScan = document.getElementById('btn-scan');
    const btnTheme = document.getElementById('btn-theme');
    const modsList = document.getElementById('mods-list');
    const consoleLogs = document.getElementById('console-logs');
    const appVersionEl = document.getElementById('app-version');
    const sptVersionEl = document.getElementById('spt-version');
    
    const detailPane = document.getElementById('detail-pane');
    const detailTitle = document.getElementById('detail-title');
    const detailContent = document.getElementById('detail-content');
    const btnCloseDetail = document.getElementById('btn-close-detail');
    
    const consoleDrawer = document.getElementById('console-drawer');
    const btnConsoleToggle = document.getElementById('btn-console-toggle');
    const btnCopyLog = document.getElementById('btn-copy-log');

    const ignoreModal = document.getElementById('ignore-modal');
    const btnCloseModal = document.getElementById('btn-close-modal');
    const ignoreModalBody = document.getElementById('ignore-modal-body');

    function init() {
        // Restore theme
        const savedTheme = localStorage.getItem('cme-theme') || 'dark';
        setTheme(savedTheme);

        // Restore filter status
        const savedFilterStatus = localStorage.getItem('cme-filter-status');
        if (savedFilterStatus) {
            state.filters.status = savedFilterStatus;
            document.querySelectorAll('.chip').forEach(c => {
                if (c.dataset.filter === savedFilterStatus) c.classList.add('active');
                else c.classList.remove('active');
            });
        }

        // Restore console state
        const savedConsole = localStorage.getItem('cme-console-collapsed') === 'true';
        if (savedConsole) toggleConsole(true);

        // Fetch initial status
        fetchStatus().then(data => {
            if (data && data.version) {
                state.meta.appVersion = data.version;
                appVersionEl.textContent = `v${data.version}`;
            }
            if (data && data.sptVersion) {
                state.meta.sptVersion = data.sptVersion;
                sptVersionEl.textContent = `SPT v${data.sptVersion}`;
                sptVersionEl.hidden = false;
            }
        }).finally(() => {
            handleScan();
        });

        setInterval(updateLastScanTime, 30000);
            
        // Event Listeners
        btnScan.addEventListener('click', handleScan);
        btnTheme.addEventListener('click', () => setTheme(state.meta.theme === 'dark' ? 'light' : 'dark'));
        btnConsoleToggle.addEventListener('click', () => toggleConsole(!state.ui.consoleCollapsed));
        btnCopyLog.addEventListener('click', handleCopyLog);
        btnCloseDetail.addEventListener('click', () => {
            document.querySelectorAll('#mods-list tr.selected').forEach(c => c.classList.remove('selected'));
            state.ui.selectedIds.clear();
            showOverview();
        });
        
        // Event delegation for mod list actions
        modsList.addEventListener('click', handleModListClick);
        detailContent.addEventListener('click', handleDetailClick);
        if (ignoreModalBody) {
            ignoreModalBody.addEventListener('click', handleDetailClick);
        }

        if (btnCloseModal) {
            btnCloseModal.addEventListener('click', () => {
                if (ignoreModal) ignoreModal.classList.add('hidden');
            });
        }
        
        if (ignoreModal) {
            ignoreModal.addEventListener('click', (e) => {
                if (e.target === ignoreModal) ignoreModal.classList.add('hidden');
            });
        }

        document.addEventListener('keydown', handleKeyboardNavigation);

        const savedSortCol = localStorage.getItem('cme-sort-column');
        const savedSortDir = localStorage.getItem('cme-sort-direction');
        if (savedSortCol) state.sort.column = savedSortCol;
        if (savedSortDir) state.sort.direction = savedSortDir;

        const searchInput = document.getElementById('search-input');
        let searchTimeout;
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    state.filters.search = e.target.value.toLowerCase();
                    render();
                }, 250);
            });
        }

        document.querySelectorAll('.chip').forEach(el => {
            el.addEventListener('click', (e) => setFilter(e.currentTarget.dataset.filter));
        });

        document.querySelectorAll('th[data-sortable]').forEach(th => {
            th.addEventListener('click', (e) => {
                const col = e.currentTarget.dataset.sortable;
                if (state.sort.column === col) {
                    state.sort.direction = state.sort.direction === 'asc' ? 'desc' : 'asc';
                } else {
                    state.sort.column = col;
                    state.sort.direction = 'asc';
                }
                localStorage.setItem('cme-sort-column', state.sort.column);
                localStorage.setItem('cme-sort-direction', state.sort.direction);
                render();
            });
        });

        const selectAll = document.getElementById('select-all');
        if (selectAll) {
            selectAll.addEventListener('change', (e) => {
                if (e.target.checked) {
                    state.filteredMods.forEach(m => state.ui.selectedIds.add(String(m.id)));
                } else {
                    state.filteredMods.forEach(m => state.ui.selectedIds.delete(String(m.id)));
                }
                render();
            });
        }

        const btnBulkOpen = document.getElementById('btn-bulk-open');
        if (btnBulkOpen) {
            btnBulkOpen.addEventListener('click', async () => {
                const selectedMods = state.mods.filter(m => state.ui.selectedIds.has(String(m.id)));
                for (const m of selectedMods) {
                    const url = m.downloadUrl || m.modUrl;
                    if (url) {
                        try {
                            await systemOpen(url);
                        } catch (e) {
                            logToConsole(`> Error opening URL for ${m.name}: ${e}`, 'error');
                        }
                    }
                }
            });
        }

        const btnBulkIgnore = document.getElementById('btn-bulk-ignore');
        if (btnBulkIgnore) {
            btnBulkIgnore.addEventListener('click', async () => {
                const selectedMods = state.mods.filter(m => state.ui.selectedIds.has(String(m.id)));
                for (const mod of selectedMods) {
                    try {
                        await ignoreMod(mod.id, mod.localVersion, mod.latestVersion);
                        logToConsole(`> Successfully ignored ${mod.id}.`, 'success');
                    } catch(err) {
                        logToConsole(`> Error ignoring mod ${mod.id}: ${err.message}`, 'error');
                    }
                }
                state.ui.selectedIds.clear();
                handleScan();
            });
        }

        const btnBulkClear = document.getElementById('btn-bulk-clear');
        if (btnBulkClear) {
            btnBulkClear.addEventListener('click', () => {
                state.ui.selectedIds.clear();
                render();
            });
        }
    }

    function handleKeyboardNavigation(e) {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

        if (e.key === 'Escape') {
            document.querySelectorAll('#mods-list tr.selected').forEach(c => c.classList.remove('selected'));
            showOverview();
        } else if (e.key === 'j' || e.key === 'k') {
            const rows = Array.from(document.querySelectorAll('#mods-list tr'));
            if (rows.length === 0) return;

            let selectedIdx = rows.findIndex(r => r.classList.contains('selected'));
            if (selectedIdx === -1) {
                selectedIdx = 0;
            } else {
                if (e.key === 'j') {
                    selectedIdx = Math.min(selectedIdx + 1, rows.length - 1);
                } else if (e.key === 'k') {
                    selectedIdx = Math.max(selectedIdx - 1, 0); 
                }
            }

            const row = rows[selectedIdx];
            document.querySelectorAll('#mods-list tr.selected').forEach(c => c.classList.remove('selected'));
            row.classList.add('selected');
            row.scrollIntoView({ block: 'nearest', behavior: 'smooth' });

            const id = row.dataset.id;
            const mod = state.mods.find(m => String(m.id) === String(id));
            if (mod) renderDetailRow(mod);
        }
    }
    
    function handleModListClick(e) {
        const checkbox = e.target.closest('.row-checkbox');
        if (checkbox) {
            if (checkbox.id === 'select-all') return;
            const id = checkbox.value;
            if (checkbox.checked) state.ui.selectedIds.add(id);
            else state.ui.selectedIds.delete(id);
            const tr = checkbox.closest('tr');
            if (tr) {
                if (checkbox.checked) tr.classList.add('selected');
                else tr.classList.remove('selected');
            }
            
            // Re-render bulk bar and select-all
            render();
            e.stopPropagation();
            return;
        }

        const tr = e.target.closest('tr');
        if (tr && tr.dataset.id) {
            const id = tr.dataset.id;
            const mod = state.mods.find(m => String(m.id) === String(id));
            if (mod) {
                document.querySelectorAll('#mods-list tr.selected').forEach(c => c.classList.remove('selected'));
                tr.classList.add('selected');
                renderDetailRow(mod);
            }
        }
    }

    async function handleDetailClick(e) {
        if (e.target.classList.contains('action-ignore')) {
            const btn = e.target;
            const id = btn.dataset.id;
            const localVer = btn.dataset.local;
            const latestVer = btn.dataset.latest;
            
            logToConsole(`> Adding ${id} to ignore list...`);
            try {
                await ignoreMod(id, localVer, latestVer);
                logToConsole(`> Successfully ignored ${id}.`, 'success');
                detailPane.classList.add('hidden');
                handleScan();
            } catch(err) {
                logToConsole(`> Error ignoring mod: ${err.message}`, 'error');
            }
        } else if (e.target.classList.contains('action-unignore')) {
            const btn = e.target;
            const id = btn.dataset.id;
            
            logToConsole(`> Removing ${id} from ignore list...`);
            try {
                await unignoreMod(id);
                logToConsole(`> Successfully un-ignored ${id}.`, 'success');
                detailPane.classList.add('hidden');
                
                const ignoreModal = document.getElementById('ignore-modal');
                if (ignoreModal && !ignoreModal.classList.contains('hidden')) {
                    import('./ui/dashboard.js').then(d => d.renderIgnoreDashboard());
                }
                
                handleScan();
            } catch(err) {
                logToConsole(`> Error un-ignoring mod: ${err.message}`, 'error');
            }
        } else if (e.target.classList.contains('action-system-open')) {
            const btn = e.target;
            const targetStr = btn.dataset.target;
            if (!targetStr) return;
            
            try {
                await systemOpen(targetStr);
            } catch(err) {
                logToConsole(`> Error opening target: ${err.message}`, 'error');
            }
        }
    }
    
    async function handleScan() {
        if (state.ui.scanning) return;
        
        state.ui.scanning = true;
        btnScan.disabled = true;
        btnScan.textContent = '[ SCANNING... ]';
        
        modsList.innerHTML = `
            <tr><td colspan="4" style="padding: 0; border: none; height: 300px;">
                <div class="scan-loader-container">
                    <div class="loader-spinner" style="display: none;"></div>
                    <div class="progress-bar-container">
                        <div class="progress-bar-fill"></div>
                    </div>
                    <div id="loader-text">[ INITIALIZING SCAN SEQUENCE... ]</div>
                </div>
            </td></tr>
        `;
        detailPane.classList.add('hidden');
        
        startLoaderAnimation();
        logToConsole('> INITIATING MOD SCAN...', 'warn');

        try {
            const results = await fetchScan();
            state.mods = results.mods || [];
            
            state.meta.lastScan = Date.now();
            updateLastScanTime();
            
            logToConsole(`> SCAN COMPLETE. ${state.mods.length} entities analyzed.`, 'success');
            render();
            
        } catch (error) {
            logToConsole(`> SCAN FAILED: ${error.message}`, 'error');
            renderEmptyState(`Scan failed: ${error.message}`, 'error');
        } finally {
            stopLoaderAnimation();
            state.ui.scanning = false;
            btnScan.disabled = false;
            btnScan.textContent = '[ SCAN LOCAL MODS ]';
        }
    }

    init();
});
