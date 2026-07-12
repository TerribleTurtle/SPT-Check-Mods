document.addEventListener('DOMContentLoaded', () => {
    // DOM Elements
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

    // Central state object (Step 4.1 requirement)
    const state = {
        mods: [],
        filteredMods: [],
        filters: { search: '', status: 'all', type: 'all' },
        sort: { column: 'status', direction: 'asc' },
        ui: { consoleCollapsed: false, scanning: false, expandedRows: new Set(), selectedIds: new Set() },
        meta: { sptVersion: null, appVersion: null, lastScan: null, theme: 'dark' }
    };

    function init() {
        // Sync theme state with restored value
        const savedTheme = localStorage.getItem('cme-theme') || 'dark';
        state.meta.theme = savedTheme;
        btnTheme.textContent = savedTheme === 'dark' ? '🌙' : '☀️';

        // Restore console state
        const savedConsole = localStorage.getItem('cme-console-collapsed') === 'true';
        if (savedConsole) toggleConsole(true);

        // Fetch initial status
        fetch('/api/status')
            .then(res => res.json())
            .then(data => {
                if (data.version) {
                    state.meta.appVersion = data.version;
                    appVersionEl.textContent = `v${data.version}`;
                }
                if (data.sptVersion) {
                    state.meta.sptVersion = data.sptVersion;
                    sptVersionEl.textContent = `SPT v${data.sptVersion}`;
                    sptVersionEl.hidden = false;
                }
            })
            .catch(err => logToConsole(`Error connecting to core: ${err}`, 'error'))
            .finally(() => {
                handleScan();
            });

        setInterval(updateLastScanTime, 30000);
            
        // Event Listeners
        btnScan.addEventListener('click', handleScan);
        btnTheme.addEventListener('click', () => setTheme(state.meta.theme === 'dark' ? 'light' : 'dark'));
        btnConsoleToggle.addEventListener('click', () => toggleConsole(!state.ui.consoleCollapsed));
        btnCopyLog.addEventListener('click', handleCopyLog);
        btnCloseDetail.addEventListener('click', () => {
            document.querySelectorAll('.mod-card.selected').forEach(c => c.classList.remove('selected'));
            state.ui.selectedIds.clear();
            if (typeof showOverview === 'function') {
                showOverview();
            } else {
                detailPane.classList.add('hidden');
            }
        });
        
        // Event delegation for mod list actions
        modsList.addEventListener('click', handleModListClick);
        detailContent.addEventListener('click', handleDetailClick);
    }

    function setTheme(theme) {
        state.meta.theme = theme;
        document.documentElement.dataset.theme = theme;
        localStorage.setItem('cme-theme', theme);
        btnTheme.textContent = theme === 'dark' ? '🌙' : '☀️';
    }

    function toggleConsole(collapsed) {
        state.ui.consoleCollapsed = collapsed;
        localStorage.setItem('cme-console-collapsed', collapsed);
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

    function handleCopyLog() {
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

    function applyFilters(mods, filters) {
        return mods; // stub for 4.2
    }
    
    function applySort(mods, sort) {
        return mods; // stub for 4.2
    }

    function render() {
        state.filteredMods = applyFilters(state.mods, state.filters);
        state.filteredMods = applySort(state.filteredMods, state.sort);
        
        renderHealthBanner(state.mods);
        renderStats(state.mods, state.filters);
        renderTable(state.filteredMods, state.sort, state.ui);
        updateTitle(state.mods);
    }

    function renderHealthBanner(mods) {
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

    function renderStats(mods, filters) {
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

    function renderStatusPill(status) {
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

    function renderVersionCell(mod) {
        if (mod.status === 'UpdateAvailable') {
            return `<span class="version-outdated" title="Current Version">v${escapeHtml(mod.localVersion)}</span> <span class="version-arrow">→</span> <span class="version-newer" title="Latest Version">v${escapeHtml(mod.latestVersion)}</span>`;
        }
        if (mod.localVersion) {
            return `<span class="version-match" title="Current Version">v${escapeHtml(mod.localVersion)}</span>`;
        }
        return '';
    }

    function renderActions(mod) {
        let actions = '';
        if (mod.downloadUrl) {
            actions += `<a href="${escapeHtml(mod.downloadUrl)}" target="_blank" class="btn-primary" style="text-decoration: none;">Download ZIP</a> `;
        }
        if (mod.modUrl) {
            actions += `<a href="${escapeHtml(mod.modUrl)}" target="_blank" class="btn-secondary" style="text-decoration: none;">View Page</a> `;
        }
        if (mod.status === 'UpdateAvailable') {
            actions += `<button class="btn-secondary action-ignore" data-id="${escapeHtml(mod.id)}" data-local="${escapeHtml(mod.localVersion)}" data-latest="${escapeHtml(mod.latestVersion)}">Ignore Update</button> `;
        }
        if (mod.isIgnored) {
             actions += `<button class="btn-secondary action-unignore" data-id="${escapeHtml(mod.id)}">Un-Ignore</button> `;
        }
        return actions;
    }

    function renderDetailRow(mod) {
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
                <p>Version <strong>${escapeHtml(mod.latestVersion)}</strong> is available. You are running <strong>${escapeHtml(mod.localVersion)}</strong>.</p>
                <div style="margin-top: 15px; display: flex; gap: 10px;">
                    ${renderActions(mod)}
                </div>
            </div>`;
        }

        if (mod.modUrl && mod.status !== 'UpdateAvailable' && !mod.isIgnored) {
            html += `<div style="margin-bottom: 20px;">
                <a href="${escapeHtml(mod.modUrl)}" target="_blank" class="btn-secondary" style="text-decoration: none;">Open Mod Page</a>
            </div>`;
        }
        
        html += `<div style="margin-bottom: 20px;">
            <h4 style="color: var(--text-secondary); margin-bottom: 8px; text-transform: uppercase; font-size: 0.8rem;">Metadata</h4>
            <div style="display: grid; grid-template-columns: 100px 1fr; gap: 8px; font-size: 0.9rem;">
                <span style="color: var(--text-muted);">Author</span>
                <span>${escapeHtml(mod.author || 'Unknown')}</span>
                <span style="color: var(--text-muted);">Local Ver</span>
                <span style="font-family: var(--font-mono);">${escapeHtml(mod.localVersion || 'N/A')}</span>
                <span style="color: var(--text-muted);">Latest Ver</span>
                <span style="font-family: var(--font-mono);">${escapeHtml(mod.latestVersion || 'N/A')}</span>
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

    function renderEmptyState(message, type = 'info') {
        modsList.innerHTML = `
            <div class="empty-state" style="color: ${type === 'error' ? 'var(--status-error)' : 'inherit'};">
                ${escapeHtml(message)}
            </div>
        `;
    }

    function renderTable(filteredMods, sort, ui) {
        if (!filteredMods || filteredMods.length === 0) {
            renderEmptyState(state.mods.length > 0 ? "No mods match your current filters." : "No mods detected in target directory.");
            return;
        }

        modsList.innerHTML = '';
        
        filteredMods.forEach(mod => {
            const card = document.createElement('div');
            card.className = 'mod-card';
            if (ui.selectedIds.has(String(mod.id))) card.classList.add('selected');
            card.dataset.id = mod.id;
            
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

            card.innerHTML = `
                <input type="checkbox" class="row-checkbox action-select" value="${escapeHtml(mod.id)}" aria-label="Select mod" ${ui.selectedIds.has(String(mod.id)) ? 'checked' : ''}>
                <div style="display:flex; align-items:center; gap:var(--space-md); flex:1;">
                    <div class="status-block ${statusClass}" title="${mod.status}" style="border-radius: 50%; box-shadow: 0 0 5px var(--status-${statusClass.split('-')[1]}); width: 12px; height: 12px; min-width: 12px;"></div>
                    <div class="mod-card-primary">
                        <div class="mod-card-title">${escapedName}</div>
                        <div class="mod-card-meta">by ${escapedAuthor} • ${typeLabel}</div>
                    </div>
                </div>
                <div class="mod-card-actions">
                    ${statusPill}
                    ${actionHtml}
                </div>
            `;
            
            modsList.appendChild(card);
        });
    }

    function updateTitle(mods) {
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

    function handleModListClick(e) {
        const checkbox = e.target.closest('.row-checkbox');
        if (checkbox) {
            const id = checkbox.value;
            if (checkbox.checked) state.ui.selectedIds.add(id);
            else state.ui.selectedIds.delete(id);
            const card = checkbox.closest('.mod-card');
            if (card) {
                if (checkbox.checked) card.classList.add('selected');
                else card.classList.remove('selected');
            }
            e.stopPropagation();
            return;
        }

        const card = e.target.closest('.mod-card');
        if (card) {
            const id = card.dataset.id;
            const mod = state.mods.find(m => String(m.id) === String(id));
            if (mod) {
                document.querySelectorAll('.mod-card.selected').forEach(c => c.classList.remove('selected'));
                card.classList.add('selected');
                // Don't add to selectedIds just from clicking the row for details
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
                const response = await fetch('/api/ignore', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ id: parseInt(id, 10), localVersion: localVer, latestVersion: latestVer })
                });
                if (response.ok) {
                    logToConsole(`> Successfully ignored ${id}.`, 'success');
                    detailPane.classList.add('hidden');
                    handleScan();
                } else {
                    throw new Error('Failed to update ignore list');
                }
            } catch(err) {
                logToConsole(`> Error ignoring mod: ${err.message}`, 'error');
            }
        } else if (e.target.classList.contains('action-unignore')) {
            const btn = e.target;
            const id = btn.dataset.id;
            
            logToConsole(`> Removing ${id} from ignore list...`);
            try {
                const response = await fetch(`/api/ignore/${id}`, {
                    method: 'DELETE'
                });
                if (response.ok) {
                    logToConsole(`> Successfully un-ignored ${id}.`, 'success');
                    detailPane.classList.add('hidden');
                    handleScan();
                } else {
                    throw new Error('Failed to update ignore list');
                }
            } catch(err) {
                logToConsole(`> Error un-ignoring mod: ${err.message}`, 'error');
            }
        }
    }

    async function handleScan() {
        if (state.ui.scanning) return;
        
        state.ui.scanning = true;
        btnScan.disabled = true;
        btnScan.textContent = '[ SCANNING... ]';
        
        modsList.innerHTML = `
            <div class="empty-state" id="loader-cell">
                <span id="loader-text">[ INITIALIZING SCAN SEQUENCE... ]</span>
            </div>
        `;
        if (typeof showOverview === 'function') {
            showOverview();
        } else {
            detailPane.classList.add('hidden');
        }
        
        startLoaderAnimation();
        logToConsole('> INITIATING MOD SCAN...', 'warn');

        try {
            const response = await fetch('/api/scan', { method: 'POST' });
            if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
            
            const results = await response.json();
            
            state.mods = results.mods || [];
            
            state.meta.lastScan = Date.now();
            updateLastScanTime();
            
            logToConsole(`> SCAN COMPLETE. ${state.mods.length} entities analyzed.`, 'success');
            render();
            if (state.ui.selectedIds.size === 0 && typeof showOverview === 'function') {
                showOverview();
            }
            
        } catch (error) {
            logToConsole(`> SCAN FAILED: ${error.message}`, 'error');
            const healthBoard = document.getElementById('health-board');
            if (healthBoard) healthBoard.style.display = 'none';
            renderEmptyState(`Scan failed: ${error.message}`, 'error');
        } finally {
            stopLoaderAnimation();
            state.ui.scanning = false;
            btnScan.disabled = false;
            btnScan.textContent = '[ SCAN LOCAL MODS ]';
        }
    }

    let loaderInterval;
    function startLoaderAnimation() {
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
        }, 1200);
    }

    function stopLoaderAnimation() {
        clearInterval(loaderInterval);
    }

    function updateLastScanTime() {
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

    async function renderIgnoreDashboard() {
        try {
            const res = await fetch('/api/ignores');
            if (!res.ok) throw new Error('Failed to fetch ignore list');
            const ignores = await res.json();
            
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

    async function showOverview() {
        detailTitle.textContent = 'Overview Dashboard';
        detailContent.innerHTML = '<div style="color: var(--text-secondary); margin-bottom: 20px;">Overview summary (Step 4.3)...</div>';
        detailPane.classList.remove('hidden');
        
        const ignoreHtml = await renderIgnoreDashboard();
        detailContent.innerHTML += ignoreHtml;
    }\n\n    function logToConsole(message, type = '') {
        const div = document.createElement('div');
        div.className = 'log-line';
        if (type === 'error') div.classList.add('log-error');
        if (type === 'success') div.classList.add('log-success');
        if (type === 'warn') div.classList.add('log-warn');
        
        div.textContent = message;
        consoleLogs.appendChild(div);
        consoleLogs.scrollTop = consoleLogs.scrollHeight;
    }

    function escapeHtml(unsafe) {
        if (unsafe == null) return '';
        return unsafe
             .toString()
             .replace(/&/g, "&amp;")
             .replace(/</g, "&lt;")
             .replace(/>/g, "&gt;")
             .replace(/"/g, "&quot;")
             .replace(/'/g, "&#039;");
    }

    init();
});
