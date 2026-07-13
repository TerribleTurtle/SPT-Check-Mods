import { logToConsole } from './utils.js';

/**
 * Fetches the current status of the application.
 * @returns {Promise<Object|null>} The status object (e.g., version, scan state) or null on error.
 */
export async function fetchStatus() {
    try {
        const response = await fetch('/api/status');
        if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
        return await response.json();
    } catch (err) {
        logToConsole(`Error connecting to core: ${err}`, 'error');
        return null;
    }
}

/**
 * Initiates a new scan and fetches the results.
 * @returns {Promise<Object>} The scan result containing the parsed mod data.
 */
export async function fetchScan() {
    const response = await fetch('/api/scan', { method: 'POST' });
    if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
    return await response.json();
}

/**
 * Fetches the list of ignored mods.
 * @returns {Promise<Array<Object>>} Array of ignored mod objects.
 */
export async function fetchIgnores() {
    const response = await fetch('/api/ignores');
    if (!response.ok) throw new Error('Failed to fetch ignore list');
    return await response.json();
}

/**
 * Adds a mod to the ignore list.
 * @param {string|number} id - Mod API ID.
 * @param {string} localVersion - Currently installed version.
 * @param {string} latestVersion - Latest version available.
 * @returns {Promise<void>}
 */
export async function ignoreMod(id, localVersion, latestVersion) {
    const response = await fetch('/api/ignore', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: parseInt(id, 10), localVersion, latestVersion })
    });
    if (!response.ok) throw new Error('Failed to update ignore list');
}

/**
 * Removes a mod from the ignore list.
 * @param {string|number} id - Mod API ID.
 * @returns {Promise<void>}
 */
export async function unignoreMod(id) {
    const response = await fetch(`/api/ignore/${id}`, {
        method: 'DELETE'
    });
    if (!response.ok) throw new Error('Failed to update ignore list');
}

/**
 * Opens a given target URL or local directory in the host system.
 * @param {string} target - The URL or local path to open.
 * @returns {Promise<void>}
 */
export async function systemOpen(target) {
    const response = await fetch('/api/system/open', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ target: target })
    });
    if (!response.ok) throw new Error(`Failed to open target`);
}

/**
 * Fetches the cached mod scan results.
 * @returns {Promise<Object|null>} The cached scan response or null if no cache exists.
 */
export async function fetchCache() {
    try {
        const response = await fetch('/api/cache');
        if (!response.ok) {
            if (response.status === 404) return null;
            throw new Error(`HTTP error! Status: ${response.status}`);
        }
        return await response.json();
    } catch (err) {
        logToConsole(`Error fetching cache: ${err}`, 'error');
        return null;
    }
}
