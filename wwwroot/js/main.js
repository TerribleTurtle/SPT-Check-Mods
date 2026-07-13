import { state , setMods, setFilteredMods, setSearchFilter, setStatusFilter, setSortColumn, setSortDirection, setScanning, setConsoleCollapsed, setLastFocus, setLastScan, setAppVersion, setSptVersion, setThemeMeta, clearSelectedIds, addSelectedId, removeSelectedId } from './state.js';;
import { fetchStatus, fetchScan, fetchCache, ignoreMod, unignoreMod, systemOpen } from './api.js';
import { logToConsole } from './utils.js';
import { setTheme, setFilter, render } from './ui/table.js';
import { toggleConsole, handleCopyLog, updateLastScanTime, startLoaderAnimation, stopLoaderAnimation, renderEmptyState, showToast } from './ui/components.js';
import { showOverview } from './ui/dashboard.js';
import { renderDetailRow } from './ui/details.js';

/**
 * Main application entry point and master-detail UI controller.
 *
 * Master-Detail View Heuristic:
 * - The left pane (master) contains the list of all filtered and sorted mods.
 * - Clicking a row marks it as 'selected' and populates the right pane (detail) via renderDetailRow.
 * - Selecting multiple rows or unselected all rows hides the detail pane and triggers showOverview() to display workspace summaries.
 * - Keyboard navigation (j/k, up/down arrows) automatically moves the selection and updates the detail pane dynamically.
 */
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
            setStatusFilter(savedFilterStatus);
            document.querySelectorAll('.chip').forEach(c => {
                if (c.dataset.filter === savedFilterStatus) c.classList.add('active');
                else c.classList.remove('active');
            });
        }


        // Fetch initial status
        fetchStatus().then(data => {
            if (data && data.version) {
                setAppVersion(data.version);
                appVersionEl.textContent = `v${data.version}`;
            }
            if (data && data.sptVersion) {
                setSptVersion(data.sptVersion);
                sptVersionEl.textContent = `SPT v${data.sptVersion}`;
                sptVersionEl.hidden = false;
            }
        }).finally(async () => {
            const cacheData = await fetchCache();
            if (cacheData && cacheData.response) {
                setMods(cacheData.response.mods || []);
                setLastScan(new Date(cacheData.cachedAtUtc).getTime());
                
                const cacheIndicator = document.getElementById('cache-indicator');
                if (cacheIndicator) {
                    cacheIndicator.style.display = 'inline-block';
                    cacheIndicator.classList.remove('hidden');
                }
                
                render();
                updateLastScanTime();
                
                // Fire silent background scan
                handleBackgroundScan();
            } else {
                handleScan();
            }
        });

        setInterval(updateLastScanTime, 30000);
            
        // Event Listeners
        btnScan.addEventListener('click', handleScan);
        btnTheme.addEventListener('click', () => setTheme(state.meta.theme === 'dark' ? 'light' : 'dark'));
        btnCloseDetail.addEventListener('click', () => {
            const selectedRow = document.querySelector('#mods-list tr.selected');
            document.querySelectorAll('#mods-list tr.selected').forEach(c => c.classList.remove('selected'));
            clearSelectedIds();
            showOverview();
            if (selectedRow) {
                const checkbox = selectedRow.querySelector('.row-checkbox');
                if (checkbox) checkbox.focus();
            }
        });
        
        // Event delegation for mod list actions
        modsList.addEventListener('click', handleModListClick);
        detailContent.addEventListener('click', handleDetailClick);
        if (ignoreModalBody) {
            ignoreModalBody.addEventListener('click', handleDetailClick);
        }

        if (btnCloseModal) {
            btnCloseModal.addEventListener('click', () => {
                if (ignoreModal) {
                    ignoreModal.classList.add('hidden');
                    if (state.ui.lastFocus) {
                        state.ui.lastFocus.focus();
                        setLastFocus(null);
                    }
                }
            });
        }
        
        if (ignoreModal) {
            ignoreModal.addEventListener('click', (e) => {
                if (e.target === ignoreModal) {
                    ignoreModal.classList.add('hidden');
                    if (state.ui.lastFocus) {
                        state.ui.lastFocus.focus();
                        setLastFocus(null);
                    }
                }
            });
        }

        document.addEventListener('keydown', handleKeyboardNavigation);

        const savedSortCol = localStorage.getItem('cme-sort-column');
        const savedSortDir = localStorage.getItem('cme-sort-direction');
        if (savedSortCol) setSortColumn(savedSortCol);
        if (savedSortDir) setSortDirection(savedSortDir);

        const searchInput = document.getElementById('search-input');
        let searchTimeout;
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    setSearchFilter(e.target.value.toLowerCase());
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
                    if (state.sort.direction === 'asc') {
                        setSortDirection('desc');
                    } else if (state.sort.direction === 'desc' && col === 'type') {
                        setSortDirection('paired');
                    } else {
                        // Clear to default 'status' asc, UNLESS we clicked 'status', then clear to 'name' asc
                        setSortColumn(col === 'status' ? 'name' : 'status');
                        setSortDirection('asc');
                    }
                } else {
                    setSortColumn(col);
                    setSortDirection('asc');
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
                    state.filteredMods.forEach(m => addSelectedId(m.id));
                } else {
                    state.filteredMods.forEach(m => removeSelectedId(m.id));
                }
                render();
            });
        }

        const btnBulkOpen = document.getElementById('btn-bulk-open');
        if (btnBulkOpen) {
            btnBulkOpen.addEventListener('click', async () => {
                const selectedMods = state.mods.filter(m => state.ui.selectedIds.has(String(m.id)));
                btnBulkOpen.disabled = true;
                btnBulkOpen.classList.add('is-loading');
                for (const m of selectedMods) {
                    const url = m.downloadUrl || m.modUrl;
                    if (url) {
                        try {
                            await systemOpen(url);
                        } catch (e) {
                            showToast(`Error opening URL for ${m.name}: ${e}`, 'error');
                        }
                    }
                }
                btnBulkOpen.disabled = false;
                btnBulkOpen.classList.remove('is-loading');
            });
        }

        const btnBulkIgnore = document.getElementById('btn-bulk-ignore');
        if (btnBulkIgnore) {
            btnBulkIgnore.addEventListener('click', async () => {
                const selectedMods = state.mods.filter(m => state.ui.selectedIds.has(String(m.id)));
                btnBulkIgnore.disabled = true;
                btnBulkIgnore.classList.add('is-loading');
                for (const mod of selectedMods) {
                    try {
                        await ignoreMod(mod.id, mod.localVersion, mod.latestVersion);
                        showToast(`Successfully ignored ${mod.id}.`, 'success');
                    } catch(err) {
                        showToast(`Error ignoring mod ${mod.id}: ${err.message}`, 'error');
                    }
                }
                btnBulkIgnore.disabled = false;
                btnBulkIgnore.classList.remove('is-loading');
                clearSelectedIds();
                handleScan();
            });
        }

        const btnBulkClear = document.getElementById('btn-bulk-clear');
        if (btnBulkClear) {
            btnBulkClear.addEventListener('click', () => {
                clearSelectedIds();
                render();
            });
        }
    }

    function handleKeyboardNavigation(e) {
        if (e.target.tagName === 'INPUT' || e.target.tagName === 'TEXTAREA') return;

        const ignoreModal = document.getElementById('ignore-modal');
        const modalOpen = ignoreModal && !ignoreModal.classList.contains('hidden');

        // Modal Focus Trap
        if (modalOpen && e.key === 'Tab') {
            const focusableElements = ignoreModal.querySelectorAll('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])');
            if (focusableElements.length > 0) {
                const first = focusableElements[0];
                const last = focusableElements[focusableElements.length - 1];

                if (e.shiftKey) {
                    if (document.activeElement === first || document.activeElement === document.body) {
                        e.preventDefault();
                        last.focus();
                    }
                } else {
                    if (document.activeElement === last || document.activeElement === document.body) {
                        e.preventDefault();
                        first.focus();
                    }
                }
            }
            return;
        }

        if (e.key === 'Escape') {
            if (modalOpen) {
                ignoreModal.classList.add('hidden');
                if (state.ui.lastFocus) {
                    state.ui.lastFocus.focus();
                    setLastFocus(null);
                }
                return;
            }

            const selectedRow = document.querySelector('#mods-list tr.selected');
            document.querySelectorAll('#mods-list tr.selected').forEach(c => c.classList.remove('selected'));
            showOverview();
            if (selectedRow) {
                const checkbox = selectedRow.querySelector('.row-checkbox');
                if (checkbox) checkbox.focus();
            }
        } else if (e.key === 'j' || e.key === 'k' || e.key === 'ArrowDown' || e.key === 'ArrowUp') {
            // Scope to table or body to avoid hijacking other interactive elements
            if (document.activeElement !== document.body && !e.target.closest('.master-list')) {
                return;
            }
            e.preventDefault();
            const rows = Array.from(document.querySelectorAll('#mods-list tr'));
            if (rows.length === 0) return;

            let selectedIdx = rows.findIndex(r => r.classList.contains('selected'));
            if (selectedIdx === -1) {
                selectedIdx = 0;
            } else {
                if (e.key === 'j' || e.key === 'ArrowDown') {
                    selectedIdx = Math.min(selectedIdx + 1, rows.length - 1);
                } else if (e.key === 'k' || e.key === 'ArrowUp') {
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
            
            const checkbox = row.querySelector('.row-checkbox');
            if (checkbox) checkbox.focus();
        } else if (e.key === 'Enter') {
            if (document.activeElement !== document.body && !e.target.closest('.master-list')) {
                return;
            }
            const selectedRow = document.querySelector('#mods-list tr.selected');
            if (selectedRow && !detailPane.classList.contains('hidden')) {
                const detailContent = document.getElementById('detail-content');
                if (detailContent) {
                    detailContent.setAttribute('tabindex', '-1');
                    detailContent.focus();
                }
            }
        }
    }
    
    function handleModListClick(e) {
        const checkbox = e.target.closest('.row-checkbox');
        if (checkbox) {
            if (checkbox.id === 'select-all') return;
            const id = checkbox.value;
            if (checkbox.checked) addSelectedId(id);
            else removeSelectedId(id);
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
                setTimeout(() => {
                    const closeBtn = document.getElementById('btn-close-detail');
                    if (closeBtn) closeBtn.focus();
                }, 50);
            }
        }
    }

    async function handleDetailClick(e) {
        if (e.target.classList.contains('action-ignore')) {
            const btn = e.target;
            const id = btn.dataset.id;
            const localVer = btn.dataset.local;
            const latestVer = btn.dataset.latest;
            
            btn.disabled = true;
            btn.classList.add('is-loading');
            try {
                await ignoreMod(id, localVer, latestVer);
                showToast(`Successfully ignored ${id}.`, 'success');
                detailPane.classList.add('hidden');
                handleScan();
            } catch(err) {
                showToast(`Error ignoring mod: ${err.message}`, 'error');
            } finally {
                btn.disabled = false;
                btn.classList.remove('is-loading');
            }
        } else if (e.target.classList.contains('action-unignore')) {
            const btn = e.target;
            const id = btn.dataset.id;
            
            btn.disabled = true;
            btn.classList.add('is-loading');
            try {
                await unignoreMod(id);
                showToast(`Successfully un-ignored ${id}.`, 'success');
                detailPane.classList.add('hidden');
                
                const ignoreModal = document.getElementById('ignore-modal');
                if (ignoreModal && !ignoreModal.classList.contains('hidden')) {
                    import('./ui/dashboard.js').then(d => d.renderIgnoreDashboard());
                }
                
                handleScan();
            } catch(err) {
                showToast(`Error un-ignoring mod: ${err.message}`, 'error');
            } finally {
                btn.disabled = false;
                btn.classList.remove('is-loading');
            }
        } else if (e.target.classList.contains('action-system-open')) {
            const btn = e.target;
            const targetStr = btn.dataset.target;
            if (!targetStr) return;
            
            try {
                await systemOpen(targetStr);
            } catch(err) {
                showToast(`Error opening target: ${err.message}`, 'error');
            }
        }
    }
    
    async function handleBackgroundScan() {
        if (state.ui.scanning) return;
        setScanning(true);
        btnScan.disabled = true;
        btnScan.textContent = '[ SCANNING... ]';
        btnScan.classList.add('is-loading');
        
        logToConsole('> INITIATING BACKGROUND SCAN...', 'warn');
        
        const tableLoader = document.getElementById('table-loading-indicator');
        if (tableLoader) tableLoader.style.display = 'block';

        const cacheIndicator = document.getElementById('cache-indicator');
        const cacheText = document.getElementById('cache-indicator-text');
        const cacheIcon = document.getElementById('cache-indicator-icon');
        if (cacheIndicator) {
            cacheIndicator.style.display = 'inline-block';
                    cacheIndicator.classList.remove('hidden');
            cacheIndicator.style.backgroundColor = 'var(--status-warning)';
            cacheIndicator.style.borderColor = 'var(--status-warning)';
            cacheIndicator.classList.add('pulsing-cache');
            
            if (cacheIcon) {
                cacheIcon.classList.add('spin-svg');
                cacheIcon.innerHTML = '<line x1="12" y1="2" x2="12" y2="6"></line><line x1="12" y1="18" x2="12" y2="22"></line><line x1="4.93" y1="4.93" x2="7.76" y2="7.76"></line><line x1="16.24" y1="16.24" x2="19.07" y2="19.07"></line><line x1="2" y1="12" x2="6" y2="12"></line><line x1="18" y1="12" x2="22" y2="12"></line><line x1="4.93" y1="19.07" x2="7.76" y2="16.24"></line><line x1="16.24" y1="4.93" x2="19.07" y2="7.76"></line>';
            }
            if (cacheText) cacheText.textContent = 'CACHED (UPDATING)';
        }

        try {
            const results = await fetchScan();
            setMods(results.mods || []);
            setLastScan(Date.now());
            
            showToast(`Scan complete. ${state.mods.length} entities analyzed.`, 'success');
            render();
        } catch (error) {
            showToast(`Scan failed: ${error.message}`, 'error');
        } finally {
            if (tableLoader) tableLoader.style.display = 'none';
            setScanning(false);
            updateLastScanTime();
            btnScan.disabled = false;
            btnScan.textContent = '[ SCAN LOCAL MODS ]';
            btnScan.classList.remove('is-loading');
        }
    }

    async function handleScan() {
        if (state.ui.scanning) return;
        
        setScanning(true);
        btnScan.disabled = true;
        btnScan.textContent = '[ SCANNING... ]';
        btnScan.classList.add('is-loading');
        
        modsList.innerHTML = `
            <tr><td colspan="5" class="p-0 border-none h-300">
                <div class="scan-loader-container">
                    <div class="loader-spinner hidden"></div>
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
            setMods(results.mods || []);
            
            setLastScan(Date.now());
            
            showToast(`Scan complete. ${state.mods.length} entities analyzed.`, 'success');
            render();
            
        } catch (error) {
            showToast(`Scan failed: ${error.message}`, 'error');
            renderEmptyState(`Scan failed: ${error.message}`, 'error');
        } finally {
            stopLoaderAnimation();
            setScanning(false);
            updateLastScanTime();
            btnScan.disabled = false;
            btnScan.textContent = '[ SCAN LOCAL MODS ]';
            btnScan.classList.remove('is-loading');
        }
    }

    init();
});
