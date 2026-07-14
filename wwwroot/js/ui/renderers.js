import { escapeHtml } from '../utils.js';

/**
 * Renders an HTML string for a mod's status pill.
 * @param {string} status - Status string.
 * @param {boolean} [isIgnored=false] - Whether the mod is ignored.
 * @param {string|null} [ignoreSource=null] - The source of the ignore ('User' or 'Remote').
 * @returns {string} HTML string of the status pill.
 */
export function renderStatusPill(status, isIgnored = false, ignoreSource = null) {
    if (isIgnored) {
        let text = 'IGNORED';
        if (ignoreSource === 'User') {
            text = 'IGNORED (YOU)';
        } else if (ignoreSource === 'Remote') {
            text = 'IGNORED (COMMUNITY)';
        }
        return `<span class="status-pill status-pill-unknown" title="Status: ${text}">${text}</span>`;
    }
    
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

/**
 * Renders an HTML string for the version cell, showing transition if outdated.
 * @param {Object} mod - Mod object.
 * @returns {string} HTML string of the version cell.
 */
export function renderVersionCell(mod) {
    if (mod.status === 'UpdateAvailable' && !mod.isIgnored) {
        return `<span class="version-outdated" title="Current Version">v${escapeHtml(mod.localVersion)}</span> <span class="version-arrow">→</span> <span class="version-newer" title="Latest Version">v${escapeHtml(mod.latestVersion)}</span>`;
    }
    if (mod.localVersion) {
        return `<span class="version-match" title="Current Version">v${escapeHtml(mod.localVersion)}</span>`;
    }
    return '';
}

/**
 * Renders an HTML string of action buttons for a mod.
 * @param {Object} mod - Mod object.
 * @returns {string} HTML string of the actions.
 */
export function renderActions(mod) {
    let actions = '';
    if (mod.downloadUrl) {
        actions += `<button class="btn-primary action-system-open" data-target="${escapeHtml(mod.downloadUrl)}">Download ZIP</button> `;
    }
    if (mod.modUrl) {
        actions += `<button class="btn-secondary action-system-open" data-target="${escapeHtml(mod.modUrl)}">Open Mod Page</button> `;
    }
    if (mod.localDirectory) {
        actions += `<button class="btn-secondary action-system-open" data-target="${escapeHtml(mod.localDirectory)}">Open Local Folder</button> `;
    }
    if (mod.status === 'UpdateAvailable') {
        actions += `<button class="btn-secondary action-ignore" data-id="${escapeHtml(mod.id)}" data-local="${escapeHtml(mod.localVersion)}" data-latest="${escapeHtml(mod.latestVersion)}">Ignore Update</button> `;
    }
    if (mod.isIgnored) {
         actions += `<button class="btn-secondary action-unignore" data-id="${escapeHtml(mod.id)}">Un-Ignore</button> `;
    }
    return actions;
}
