import { escapeHtml } from '../utils.js';
import { renderActions } from './renderers.js';

/**
 * Renders the detail pane for a selected mod, showing metadata, dependencies, and warnings.
 * @param {Object} mod - The mod object to render.
 */
export function renderDetailRow(mod) {
    const detailTitle = document.getElementById('detail-title');
    const detailContent = document.getElementById('detail-content');
    const detailPane = document.getElementById('detail-pane');
    
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
            <p>Version <strong class="ver-ok">${escapeHtml(mod.latestVersion)}</strong> is available. You are running <strong class="ver-warn">${escapeHtml(mod.localVersion)}</strong>.</p>
            <div style="margin-top: 15px; display: flex; gap: 10px;">
                ${renderActions(mod)}
            </div>
        </div>`;
    }

    let generalActions = '';
    if (mod.status !== 'UpdateAvailable' && !mod.isIgnored) {
        generalActions = renderActions(mod);
    }
    if (generalActions) {
        html += `<div style="margin-bottom: 20px; display: flex; gap: 10px;">
            ${generalActions}
        </div>`;
    }
    
    let localVerClass = 'ver-ok';
    let latestVerClass = 'ver-ok';
    if (mod.status === 'UpdateAvailable' || mod.isIgnored) {
        localVerClass = 'ver-warn';
    } else if (mod.status === 'UpdateBlocked' || mod.status === 'Incompatible') {
        localVerClass = 'ver-error';
    } else if (mod.status === 'NewerInstalled') {
        localVerClass = 'ver-info';
    }

    html += `<div style="margin-bottom: 20px;">
        <h4 style="color: var(--text-secondary); margin-bottom: 8px; text-transform: uppercase; font-size: 0.8rem;">Metadata</h4>
        <div style="display: grid; grid-template-columns: 100px 1fr; gap: 8px; font-size: 0.9rem;">
            <span style="color: var(--text-muted);">Author</span>
            <span>${escapeHtml(mod.author || 'Unknown')}</span>
            <span style="color: var(--text-muted);">Local Ver</span>
            <span class="${localVerClass}">${escapeHtml(mod.localVersion || 'N/A')}</span>
            <span style="color: var(--text-muted);">Latest Ver</span>
            <span class="${latestVerClass}">${escapeHtml(mod.latestVersion || 'N/A')}</span>
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

    if (mod.loadWarnings && mod.loadWarnings.length > 0) {
        html += `<div style="background: var(--status-warning-bg); border: 1px solid var(--status-warning); padding: 15px; border-radius: var(--radius-md); margin-bottom: 20px;">
            <h3 style="color: var(--status-warning); margin-bottom: 10px;">Mod Warnings</h3>
            <ul style="margin-left: 20px; font-size: 0.9rem; color: var(--text-primary);">`;
        mod.loadWarnings.forEach(w => {
            html += `<li style="margin-bottom: 4px;">${escapeHtml(w)}</li>`;
        });
        html += `</ul></div>`;
    }
    
    detailContent.innerHTML = html;
    detailPane.classList.remove('hidden');
}
