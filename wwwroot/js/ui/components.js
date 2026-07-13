import { state } from '../state.js';
import { escapeHtml, logToConsole } from '../utils.js';
import { setFilter } from './table.js';

/**
 * Renders an empty state message in the mod table.
 * @param {string} message - The message to display.
 * @param {string} [type='info'] - The type of message ('info', 'error').
 * @param {boolean} [isFilterEmpty=false] - Whether the empty state is due to filtering.
 */
export function renderEmptyState(message, type = 'info', isFilterEmpty = false) {
    const modsList = document.getElementById('mods-list');
    if (!modsList) return;
    modsList.innerHTML = `
        <tr>
            <td colspan="5">
                <div class="empty-state" style="color: ${type === 'error' ? 'var(--status-error)' : 'inherit'};">
                    ${escapeHtml(message)}
                    ${isFilterEmpty ? '<br><br><button class="btn-secondary" id="btn-clear-filters">Clear Filters</button>' : ''}
                </div>
            </td>
        </tr>
    `;
    if (isFilterEmpty) {
        const btnClear = document.getElementById('btn-clear-filters');
        if (btnClear) {
            btnClear.addEventListener('click', () => {
                const searchInput = document.getElementById('search-input');
                if (searchInput) searchInput.value = '';
                state.filters.search = '';
                setFilter('all');
            });
        }
    }
}

/**
 * Toggles the visibility of the UI console drawer.
 * @param {boolean} collapsed - Whether the console should be collapsed.
 */
export function toggleConsole(collapsed) {
    state.ui.consoleCollapsed = collapsed;
    localStorage.setItem('cme-console-collapsed', collapsed);
    const consoleDrawer = document.getElementById('console-drawer');
    const btnConsoleToggle = document.getElementById('btn-console-toggle');
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

/**
 * Copies the contents of the console logs to the clipboard.
 */
export function handleCopyLog() {
    const consoleLogs = document.getElementById('console-logs');
    const btnCopyLog = document.getElementById('btn-copy-log');
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

/**
 * Updates the "Last scanned" time display in the UI.
 */
export function updateLastScanTime() {
    if (!state.meta.lastScan) return;
    const lastScanEl = document.getElementById('last-scan-time');
    
    const seconds = Math.floor((Date.now() - state.meta.lastScan) / 1000);
    let text = 'Just now';
    if (seconds > 59) {
        const minutes = Math.floor(seconds / 60);
        text = `${minutes}m ago`;
    } else if (seconds > 10) {
        text = `${seconds}s ago`;
    }
    
    if (lastScanEl) {
        lastScanEl.textContent = `Last scanned: ${text}`;
    }
    
    const cacheIndicator = document.getElementById('cache-indicator');
    const cacheText = document.getElementById('cache-indicator-text');
    if (cacheIndicator && cacheText && !state.ui.scanning && state.meta.lastScan > 0) {
        cacheIndicator.style.display = 'inline-block';
        cacheIndicator.style.backgroundColor = 'var(--status-success)';
        cacheIndicator.style.borderColor = 'var(--status-success)';
        cacheIndicator.classList.remove('pulsing-cache');
        
        const icon = document.getElementById('cache-indicator-icon');
        if (icon) {
            icon.classList.remove('spin-svg');
            icon.innerHTML = '<polyline points="20 6 9 17 4 12"></polyline>';
        }
        
        cacheText.textContent = `UPDATED ${text.toUpperCase()}`;
    }
}

let loaderState = { active: false, interval: null };
/**
 * Starts the loader animation with asymptotic progression to simulate loading.
 * Math heuristic:
 * The progress bar increments by 5% of the remaining distance to a max of 95%,
 * ensuring it never reaches 100% until explicitly stopped, slowing down as it nears 95%.
 */
export function startLoaderAnimation() {
    if (loaderState.active) return;
    loaderState.active = true;
    const loaderText = document.getElementById('loader-text');
    const fill = document.querySelector('.progress-bar-fill');
    if (!loaderText || !fill) return;

    fill.classList.remove('done');
    fill.classList.add('pulsing');
    fill.style.transition = 'width 0.2s cubic-bezier(0.25, 1, 0.5, 1)';
    fill.style.width = '0%';
    
    loaderText.textContent = '> Initiating workspace scan...';

    let currentP = 0;
    const targetMax = 95;
    
    // Increment the progress bar asymptotically
    loaderState.interval = setInterval(() => {
        if (!loaderState.active) {
            clearInterval(loaderState.interval);
            return;
        }
        
        // The closer we get to max, the slower we grow
        if (currentP < targetMax) {
            const remaining = targetMax - currentP;
            const step = Math.max(remaining * 0.05, 0.1); 
            currentP += step;
            
            if (currentP > targetMax) {
                currentP = targetMax;
            }
            
            if (currentP > 20 && loaderText.textContent.includes('Initiating')) {
                loaderText.textContent = '> Indexing local mod directories...';
            } else if (currentP > 50 && loaderText.textContent.includes('Indexing')) {
                loaderText.textContent = '> Reconciling version hashes...';
            } else if (currentP > 80 && loaderText.textContent.includes('Reconciling')) {
                loaderText.textContent = '> Analyzing dependencies...';
            }
            
            fill.style.width = currentP + '%';
        }
    }, 100);
}

/**
 * Stops the loader animation and sets the progress bar to 100%.
 */
export function stopLoaderAnimation() {
    loaderState.active = false;
    if (loaderState.interval) {
        clearInterval(loaderState.interval);
        loaderState.interval = null;
    }
    const loaderText = document.getElementById('loader-text');
    const fill = document.querySelector('.progress-bar-fill');
    if (loaderText) loaderText.textContent = '> SCAN COMPLETE';
    if (fill) {
        fill.classList.remove('pulsing');
        fill.classList.add('done');
        fill.style.transition = 'width 0.5s cubic-bezier(0.25, 1, 0.5, 1)';
        fill.style.width = '100%';
    }
}


/**
 * Shows a toast message.
 * @param {string} message - The message.
 * @param {string} type - 'success', 'error', 'info', 'warning'
 */
export function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    
    container.appendChild(toast);
    
    // Animate in
    setTimeout(() => toast.classList.add('show'), 10);
    
    // Remove after 3 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}
