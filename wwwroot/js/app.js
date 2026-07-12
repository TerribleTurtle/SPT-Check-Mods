document.addEventListener('DOMContentLoaded', () => {
    const btnScan = document.getElementById('btn-scan');
    const tableBody = document.getElementById('mods-body');
    const consoleLogs = document.getElementById('console-logs');
    const appVersion = document.getElementById('app-version');

    // Fetch initial status to get app version
    fetch('/api/status')
        .then(res => res.json())
        .then(data => {
            if (data.version) {
                appVersion.textContent = `v${data.version}`;
            }
        })
        .catch(err => logToConsole(`Error connecting to core: ${err}`, 'error'));

    btnScan.addEventListener('click', async () => {
        btnScan.disabled = true;
        btnScan.textContent = '[ SCANNING... ]';
        
        tableBody.innerHTML = `
            <tr class="empty-row">
                <td colspan="8" id="loader-cell">
                    <span id="loader-text">[ INITIALIZING SCAN SEQUENCE... ]</span>
                </td>
            </tr>
        `;
        
        startLoaderAnimation();
        
        logToConsole('> INITIATING MOD SCAN...', 'warn');

        try {
            const response = await fetch('/api/scan', { method: 'POST' });
            if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
            
            const results = await response.json();
            stopLoaderAnimation();
            renderStats(results.mods);
            renderMods(results.mods);
            logToConsole(`> SCAN COMPLETE. ${results.mods.length} entities analyzed.`, 'success');
            
        } catch (error) {
            stopLoaderAnimation();
            logToConsole(`> SCAN FAILED: ${error.message}`, 'error');
            document.getElementById('stats-container').style.display = 'none';
            tableBody.innerHTML = `
                <tr class="empty-row row-error">
                    <td colspan="8">Scan failed. Check system logs.</td>
                </tr>
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
        const statsContainer = document.getElementById('stats-container');
        const clientMods = mods.filter(m => !m.isServerMod).length;
        const serverMods = mods.filter(m => m.isServerMod).length;
        
        const outOfDateMods = mods.filter(m => m.status === 'UpdateAvailable');
        
        document.getElementById('stat-client').textContent = clientMods;
        document.getElementById('stat-server').textContent = serverMods;
        document.getElementById('stat-outdated').textContent = outOfDateMods.length;
        
        const btnOpenAll = document.getElementById('btn-open-all');
        btnOpenAll.onclick = () => {
            outOfDateMods.forEach(m => {
                if (m.modUrl) window.open(m.modUrl, '_blank');
            });
        };
        
        btnOpenAll.style.display = outOfDateMods.length > 0 ? 'inline-block' : 'none';
        statsContainer.style.display = 'flex';
    }

    function renderMods(mods) {
        if (!mods || mods.length === 0) {
            tableBody.innerHTML = `
                <tr class="empty-row">
                    <td colspan="8">No mods detected in target directory.</td>
                </tr>
            `;
            return;
        }

        tableBody.innerHTML = '';
        
        mods.forEach(mod => {
            const tr = document.createElement('tr');
            const detailsTr = document.createElement('tr');
            detailsTr.className = 'details-row';
            
            // Determine status
            let statusClass = 'status-unknown';
            let rowClass = '';
            let actionHtml = '';
            
            let hasDetails = false;
            let detailsHtml = '<div class="details-container">';

            if (mod.status === 'UpToDate') {
                statusClass = 'status-ok';
            } else if (mod.status === 'NewerInstalled') {
                statusClass = 'status-newer';
            } else if (mod.status === 'UpdateAvailable') {
                statusClass = 'status-warn';
                rowClass = 'row-warn';
                
                if (mod.modUrl) {
                    actionHtml += `<a href="${escapeHtml(mod.modUrl)}" target="_blank" rel="noopener noreferrer" class="btn-secondary">[ PAGE ]</a> `;
                }
                if (mod.downloadUrl) {
                    actionHtml += `<a href="${escapeHtml(mod.downloadUrl)}" target="_blank" rel="noopener noreferrer" class="btn-secondary">[ ZIP ]</a> `;
                }
                actionHtml += `<button class="btn-secondary" onclick="ignoreMod('${escapeHtml(mod.id)}', '${escapeHtml(mod.localVersion)}', '${escapeHtml(mod.latestVersion)}')">[ IGNORE ]</button>`;
            } else if (mod.status === 'UpdateBlocked') {
                statusClass = 'status-error';
                rowClass = 'row-error';
                hasDetails = true;
                
                detailsHtml += `<div class="details-section">
                    <div class="details-section-title">Update Blocked</div>
                    <div><span class="badge badge-error">BLOCKED</span> ${escapeHtml(mod.blockReason ? mod.blockReason.replace(/_/g, ' ') : 'Unknown reason')}</div>`;
                
                if (mod.blockingMods && mod.blockingMods.length > 0) {
                    detailsHtml += `<div style="margin-top: 5px; color: var(--text-muted); font-size: 12px;">Blocked by:</div>`;
                    mod.blockingMods.forEach(b => {
                        detailsHtml += `<div>- ${escapeHtml(b.name)} (${escapeHtml(b.constraint)})</div>`;
                    });
                }
                detailsHtml += `</div>`;
                
            } else if (mod.status === 'Incompatible' || mod.status === 'Error' || mod.status === 'NoVersionsFound') {
                statusClass = 'status-error';
                rowClass = 'row-error';
                
                if (mod.status === 'Incompatible') {
                    hasDetails = true;
                    detailsHtml += `<div class="details-section">
                        <div class="details-section-title">Incompatible with SPT</div>
                        <div><span class="badge badge-error">INCOMPATIBLE</span> ${escapeHtml(mod.incompatibilityReason || 'Unknown')}</div>`;
                    
                    if (mod.compatibleVersion) {
                        detailsHtml += `<div style="margin-top: 5px;">Latest compatible version: <span class="badge badge-success">${escapeHtml(mod.compatibleVersion)}</span></div>`;
                        if (mod.downloadUrl) {
                            detailsHtml += `<div style="margin-top: 5px;"><a href="${escapeHtml(mod.downloadUrl)}" target="_blank" rel="noopener noreferrer" class="btn-secondary">[ DOWNLOAD COMPATIBLE ZIP ]</a></div>`;
                        }
                    } else {
                        detailsHtml += `<div style="margin-top: 5px; color: var(--accent-error);">No compatible version available for this SPT version.</div>`;
                    }
                    detailsHtml += `</div>`;
                }
            }
            
            // Dependency changes
            if (mod.addedDependencies && mod.addedDependencies.length > 0) {
                hasDetails = true;
                detailsHtml += `<div class="details-section"><div class="details-section-title">Required Dependencies</div>`;
                mod.addedDependencies.forEach(dep => {
                    let depState = '';
                    if (dep.installState === 'NotInstalled') {
                        depState = `<span class="badge badge-error">MISSING</span> download v${escapeHtml(dep.recommendedVersion)}`;
                    } else if (dep.installState === 'InstalledOutdated') {
                        depState = `<span class="badge badge-warn">OUTDATED</span> installed v${escapeHtml(dep.installedVersion || '?')}, needs v${escapeHtml(dep.recommendedVersion)}`;
                    } else {
                        depState = `<span class="badge badge-neutral">SATISFIED</span> v${escapeHtml(dep.installedVersion || dep.recommendedVersion)}`;
                    }
                    
                    let linkHtml = dep.downloadLink ? `<a href="${escapeHtml(dep.downloadLink)}" target="_blank" class="dep-link">${escapeHtml(dep.name)}</a>` : escapeHtml(dep.name);
                    
                    detailsHtml += `<div>[+] ${linkHtml} - ${depState}</div>`;
                    if (dep.conflict) {
                        detailsHtml += `<div style="color: var(--accent-error); margin-left: 20px; font-size: 12px;">Version constraint conflict reported.</div>`;
                    }
                });
                detailsHtml += `</div>`;
            }
            
            if (mod.removedDependencies && mod.removedDependencies.length > 0) {
                hasDetails = true;
                detailsHtml += `<div class="details-section"><div class="details-section-title">Removed Dependencies</div>`;
                mod.removedDependencies.forEach(dep => {
                    detailsHtml += `<div>[-] <span style="color: var(--text-muted);">${escapeHtml(dep.name)} no longer required (was v${escapeHtml(dep.installedVersion || dep.recommendedVersion)})</span></div>`;
                });
                detailsHtml += `</div>`;
            }
            
            detailsHtml += '</div>';
            detailsTr.innerHTML = `<td colspan="8" style="padding: 0;">${detailsHtml}</td>`;

            if (rowClass) {
                tr.classList.add(rowClass);
            }
            
            const escapedName = escapeHtml(mod.name || 'Unknown');
            const escapedAuthor = escapeHtml(mod.author || 'Unknown');
            const escapedLocal = escapeHtml(mod.localVersion || '---');
            const escapedLatest = escapeHtml(mod.latestVersion || '---');
            
            const nameHtml = mod.modUrl ? `<a href="${escapeHtml(mod.modUrl)}" target="_blank" rel="noopener noreferrer" class="mod-link">${escapedName}</a>` : escapedName;
            const typeLabel = mod.isServerMod ? '<span class="type-tag server-tag">SRV</span>' : '<span class="type-tag client-tag">CLI</span>';
            
            let expandBtn = '';
            if (hasDetails) {
                expandBtn = `<button class="expand-btn" aria-label="Expand details">▼</button>`;
                tr.style.cursor = 'pointer';
                tr.addEventListener('click', (e) => {
                    // Don't toggle if they clicked a button/link in the row
                    if (e.target.tagName !== 'BUTTON' && e.target.tagName !== 'A') {
                        detailsTr.classList.toggle('show');
                        const btn = tr.querySelector('.expand-btn');
                        if (btn) btn.textContent = detailsTr.classList.contains('show') ? '▲' : '▼';
                    }
                });
            }

            tr.innerHTML = `
                <td class="col-expand">${expandBtn}</td>
                <td class="col-status">
                    <span class="status-block ${statusClass}" title="${mod.status}" aria-label="Status: ${mod.status}"></span>
                </td>
                <td class="col-type">${typeLabel}</td>
                <td class="col-name">${nameHtml}</td>
                <td class="col-author">${escapedAuthor}</td>
                <td class="col-version">${escapedLocal}</td>
                <td class="col-version">${escapedLatest}</td>
                <td class="col-actions">${actionHtml}</td>
            `;
            
            tableBody.appendChild(tr);
            if (hasDetails) {
                tableBody.appendChild(detailsTr);
            }
        });
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
                // Re-trigger scan or update row
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
