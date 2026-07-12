document.addEventListener('DOMContentLoaded', () => {
    const btnScan = document.getElementById('btn-scan');
    const modsList = document.getElementById('mods-list');
    const consoleLogs = document.getElementById('console-logs');
    const appVersion = document.getElementById('app-version');
    
    // Master-Detail elements
    const detailPane = document.getElementById('detail-pane');
    const detailTitle = document.getElementById('detail-title');
    const detailContent = document.getElementById('detail-content');
    const btnCloseDetail = document.getElementById('btn-close-detail');

    // Fetch initial status to get app version
    fetch('/api/status')
        .then(res => res.json())
        .then(data => {
            if (data.version) {
                appVersion.textContent = `v${data.version}`;
            }
        })
        .catch(err => logToConsole(`Error connecting to core: ${err}`, 'error'));

    btnCloseDetail.addEventListener('click', () => {
        detailPane.classList.add('hidden');
        document.querySelectorAll('.mod-card.selected').forEach(c => c.classList.remove('selected'));
    });

    btnScan.addEventListener('click', async () => {
        btnScan.disabled = true;
        btnScan.textContent = '[ SCANNING... ]';
        
        modsList.innerHTML = `
            <div class="empty-state" id="loader-cell">
                <span id="loader-text">[ INITIALIZING SCAN SEQUENCE... ]</span>
            </div>
        `;
        detailPane.classList.add('hidden');
        
        startLoaderAnimation();
        
        logToConsole('> INITIATING MOD SCAN...', 'warn');

        try {
            const response = await fetch('/api/scan', { method: 'POST' });
            if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
            
            const results = await response.json();
            stopLoaderAnimation();
            renderStats(results.mods);
            renderMods(results.mods);
            
            // Update last scan time
            const lastScanEl = document.getElementById('last-scan-time');
            if (lastScanEl) {
                const now = new Date();
                lastScanEl.textContent = `Last scan: ${now.toLocaleTimeString()}`;
            }
            
            logToConsole(`> SCAN COMPLETE. ${results.mods.length} entities analyzed.`, 'success');
            
        } catch (error) {
            stopLoaderAnimation();
            logToConsole(`> SCAN FAILED: ${error.message}`, 'error');
            document.getElementById('health-board').style.display = 'none';
            modsList.innerHTML = `
                <div class="empty-state" style="color: var(--status-error);">
                    Scan failed. Check system logs.
                </div>
            `;
        } finally {
            btnScan.disabled = false;
            btnScan.textContent = '[ SCAN LOCAL MODS ]';
        }
    });

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
            if (document.getElementById('loader-text')) {
                document.getElementById('loader-text').textContent = states[i];
            }
        }, 1200);
    }

    function stopLoaderAnimation() {
        clearInterval(loaderInterval);
    }

    function renderStats(mods) {
        const healthBoard = document.getElementById('health-board');
        if (!healthBoard) return;
        
        const totalMods = mods.length;
        const upToDateMods = mods.filter(m => m.status === 'UpToDate' || m.status === 'NewerInstalled');
        const actionableMods = mods.length - upToDateMods.length;
        
        const elTotal = document.getElementById('stat-total');
        if (elTotal) elTotal.textContent = totalMods;
        
        const elOutdated = document.getElementById('stat-outdated');
        if (elOutdated) elOutdated.textContent = actionableMods;
        
        const elOk = document.getElementById('stat-ok');
        if (elOk) elOk.textContent = upToDateMods.length;
        
        // Use flex/grid based on window width or let css handle it
        healthBoard.style.display = window.innerWidth > 768 ? 'grid' : 'flex';
        if (window.innerWidth <= 768) {
            healthBoard.style.flexDirection = 'column';
        }
    }

    function renderMods(mods) {
        if (!mods || mods.length === 0) {
            modsList.innerHTML = `
                <div class="empty-state">No mods detected in target directory.</div>
            `;
            return;
        }

        modsList.innerHTML = '';
        
        mods.forEach(mod => {
            const card = document.createElement('div');
            card.className = 'mod-card';
            
            // Determine status
            let statusClass = 'status-unknown';
            let actionHtml = '';

            if (mod.status === 'UpToDate') {
                statusClass = 'status-ok';
            } else if (mod.status === 'NewerInstalled') {
                statusClass = 'status-newer';
            } else if (mod.status === 'UpdateAvailable') {
                statusClass = 'status-warn';
                actionHtml += `<span class="version-outdated">v${escapeHtml(mod.localVersion)} <span class="version-arrow">→</span> v${escapeHtml(mod.latestVersion)}</span>`;
            } else if (mod.status === 'UpdateBlocked' || mod.status === 'Incompatible' || mod.status === 'Error' || mod.status === 'NoVersionsFound') {
                statusClass = 'status-error';
                actionHtml += `<span class="badge badge-error">ISSUE DETECTED</span>`;
            }
            
            if (!actionHtml && mod.localVersion) {
                actionHtml = `<span class="version-match">v${escapeHtml(mod.localVersion)}</span>`;
            }
            
            const escapedName = escapeHtml(mod.name || 'Unknown');
            const escapedAuthor = escapeHtml(mod.author || 'Unknown');
            const typeLabel = mod.isServerMod ? '<span style="color: var(--status-success); font-weight: 600;">Server</span>' : '<span style="color: var(--status-info); font-weight: 600;">Client</span>';
            
            card.innerHTML = `
                <input type="checkbox" class="row-checkbox" value="${escapeHtml(mod.id)}" aria-label="Select mod" onclick="event.stopPropagation()">
                <div class="status-block ${statusClass}" title="${mod.status}" style="border-radius: 50%; box-shadow: 0 0 5px var(--status-${statusClass.split('-')[1]});"></div>
                <div class="mod-card-primary">
                    <div class="mod-card-title">${escapedName}</div>
                    <div class="mod-card-meta">by ${escapedAuthor} • ${typeLabel}</div>
                </div>
                <div class="mod-card-actions">
                    ${actionHtml}
                </div>
            `;
            
            card.addEventListener('click', () => {
                document.querySelectorAll('.mod-card.selected').forEach(c => c.classList.remove('selected'));
                card.classList.add('selected');
                showModDetails(mod);
            });
            
            modsList.appendChild(card);
        });
    }

    function showModDetails(mod) {
        detailTitle.textContent = mod.name || 'Unknown Mod';
        
        let html = '';
        
        // Status Alert
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
        } else if (mod.status === 'UpdateAvailable') {
            html += `<div style="background: var(--status-warning-bg); border: 1px solid var(--status-warning); padding: 15px; border-radius: var(--radius-md); margin-bottom: 20px;">
                <h3 style="color: var(--status-warning); margin-bottom: 10px;">Update Available</h3>
                <p>Version <strong>${escapeHtml(mod.latestVersion)}</strong> is available. You are running <strong>${escapeHtml(mod.localVersion)}</strong>.</p>
                <div style="margin-top: 15px; display: flex; gap: 10px;">
                    ${mod.downloadUrl ? `<a href="${escapeHtml(mod.downloadUrl)}" target="_blank" class="btn-primary" style="text-decoration: none;">Download ZIP</a>` : ''}
                    ${mod.modUrl ? `<a href="${escapeHtml(mod.modUrl)}" target="_blank" class="btn-secondary" style="text-decoration: none;">View Page</a>` : ''}
                    <button class="btn-secondary" onclick="ignoreMod('${escapeHtml(mod.id)}', '${escapeHtml(mod.localVersion)}', '${escapeHtml(mod.latestVersion)}')">Ignore Update</button>
                </div>
            </div>`;
        }

        // Links
        if (mod.modUrl && mod.status !== 'UpdateAvailable') {
            html += `<div style="margin-bottom: 20px;">
                <a href="${escapeHtml(mod.modUrl)}" target="_blank" class="btn-secondary" style="text-decoration: none;">Open Mod Page</a>
            </div>`;
        }
        
        // Metadata
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
                <span>${mod.isServerMod ? 'Server Mod' : 'Client Mod'}</span>
            </div>
        </div>`;
        
        // Dependencies
        if (mod.addedDependencies && mod.addedDependencies.length > 0) {
            html += `<div style="margin-bottom: 20px;">
                <h4 style="color: var(--text-secondary); margin-bottom: 8px; text-transform: uppercase; font-size: 0.8rem;">Required Dependencies</h4>
                <div style="display: flex; flex-direction: column; gap: 8px;">`;
            
            mod.addedDependencies.forEach(dep => {
                let badge = '';
                if (dep.installState === 'NotInstalled') badge = `<span class="badge badge-error">MISSING</span>`;
                else if (dep.installState === 'InstalledOutdated') badge = `<span class="badge badge-warn">OUTDATED</span>`;
                else badge = `<span class="badge badge-neutral">SATISFIED</span>`;
                
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

    window.ignoreMod = async function(modId, localVer, latestVer) {
        logToConsole(`> Adding ${modId} to ignore list...`);
        try {
            const response = await fetch('/api/ignore', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: parseInt(modId, 10), localVersion: localVer, latestVersion: latestVer })
            });
            if (response.ok) {
                logToConsole(`> Successfully ignored ${modId}.`, 'success');
                btnScan.click();
            } else {
                throw new Error('Failed to update ignore list');
            }
        } catch(e) {
            logToConsole(`> Error ignoring mod: ${e.message}`, 'error');
        }
    }

    function logToConsole(message, type = '') {
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
});
