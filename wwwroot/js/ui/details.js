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
        html += `<div class="alert-box alert-error">
            <h3 class="text-error mb-2">Update Blocked</h3>
            <p>${escapeHtml(mod.blockReason ? mod.blockReason.replace(/_/g, ' ') : 'Unknown reason')}</p>
        </div>`;
    } else if (mod.status === 'Incompatible') {
        html += `<div class="alert-box alert-error">
            <h3 class="text-error mb-2">Incompatible with SPT</h3>
            <p>${escapeHtml(mod.incompatibilityReason || 'Unknown')}</p>
            ${mod.compatibleVersion ? `<p class="mt-2">Latest compatible version: <strong>${escapeHtml(mod.compatibleVersion)}</strong></p>` : ''}
        </div>`;
    } else if (mod.status === 'UpdateAvailable' || mod.isIgnored) {
        let ignoredText = 'Update Ignored';
        if (mod.ignoreSource === 'User') {
            ignoredText = 'Update Ignored (You)';
        } else if (mod.ignoreSource === 'Remote') {
            ignoredText = 'Update Ignored (Community)';
        }
        html += `<div class="alert-box alert-warning">
            <h3 class="text-warning mb-2">${mod.isIgnored ? ignoredText : 'Update Available'}</h3>
            <p>Version <strong class="ver-ok">${escapeHtml(mod.latestVersion)}</strong> is available. You are running <strong class="ver-warn">${escapeHtml(mod.localVersion)}</strong>.</p>
            <div class="mt-3 flex gap-sm">
                ${renderActions(mod)}
            </div>
        </div>`;
    }

    let generalActions = '';
    if (mod.status !== 'UpdateAvailable' && !mod.isIgnored) {
        generalActions = renderActions(mod);
    }
    if (generalActions) {
        html += `<div class="mb-4 flex gap-sm">
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

    html += `<div class="mb-4">
        <h4 class="detail-section-title">Metadata</h4>
        <div class="detail-grid">
            <span class="text-muted">Author</span>
            <span>${escapeHtml(mod.author || 'Unknown')}</span>
            <span class="text-muted">Local Ver</span>
            <span class="${localVerClass}">${escapeHtml(mod.localVersion || 'N/A')}</span>
            <span class="text-muted">Latest Ver</span>
            <span class="${latestVerClass}">${escapeHtml(mod.latestVersion || 'N/A')}</span>
            <span class="text-muted">Type</span>
            <span title="${mod.isServerMod ? 'Server Mod' : 'Client Mod'}">${mod.isServerMod ? 'Server Mod' : 'Client Mod'}</span>
        </div>
    </div>`;
    
    if (mod.addedDependencies && mod.addedDependencies.length > 0) {
        html += `<div class="mb-4">
            <h4 class="detail-section-title">Required Dependencies</h4>
            <div class="flex flex-col gap-sm">`;
        
        mod.addedDependencies.forEach(dep => {
            let badge = '';
            if (dep.installState === 'NotInstalled') badge = `<span class="badge badge-error" title="Missing Dependency">MISSING</span>`;
            else if (dep.installState === 'InstalledOutdated') badge = `<span class="badge badge-warning" title="Outdated Dependency">OUTDATED</span>`;
            else badge = `<span class="badge badge-neutral" title="Satisfied Dependency">SATISFIED</span>`;
            
            let link = dep.downloadLink ? `<a href="${escapeHtml(dep.downloadLink)}" target="_blank" class="dep-link">${escapeHtml(dep.name)}</a>` : escapeHtml(dep.name);
            
            html += `<div class="dep-card">
                <div class="flex justify-between items-center">
                    <span>${link}</span>
                    ${badge}
                </div>
                <div class="text-sm text-muted mt-1">Requires v${escapeHtml(dep.recommendedVersion)}</div>
            </div>`;
        });
        html += `</div></div>`;
    }

    if (mod.blockingMods && mod.blockingMods.length > 0) {
         html += `<div class="mb-4">
            <h4 class="detail-section-title">Blocked By</h4>
            <ul class="detail-list">`;
         mod.blockingMods.forEach(b => {
             html += `<li>${escapeHtml(b.name)} (constraint: ${escapeHtml(b.constraint)})</li>`;
         });
         html += `</ul></div>`;
    }

    if (mod.loadWarnings && mod.loadWarnings.length > 0) {
        html += `<div class="alert-box alert-warning">
            <h3 class="text-warning mb-2">Mod Warnings</h3>
            <ul class="detail-list ml-5">`;
        mod.loadWarnings.forEach(w => {
            html += `<li class="mb-1">${escapeHtml(w)}</li>`;
        });
        html += `</ul></div>`;
    }
    
    detailContent.innerHTML = html;
    detailPane.classList.remove('hidden');
    
    const overviewPane = document.getElementById('workspace-overview');
    if (overviewPane) {
        overviewPane.classList.add('hidden');
    }
}
