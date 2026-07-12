import { logToConsole } from './utils.js';

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

export async function fetchScan() {
    const response = await fetch('/api/scan', { method: 'POST' });
    if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);
    return await response.json();
}

export async function fetchIgnores() {
    const response = await fetch('/api/ignores');
    if (!response.ok) throw new Error('Failed to fetch ignore list');
    return await response.json();
}

export async function ignoreMod(id, localVersion, latestVersion) {
    const response = await fetch('/api/ignore', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ id: parseInt(id, 10), localVersion, latestVersion })
    });
    if (!response.ok) throw new Error('Failed to update ignore list');
}

export async function unignoreMod(id) {
    const response = await fetch(`/api/ignore/${id}`, {
        method: 'DELETE'
    });
    if (!response.ok) throw new Error('Failed to update ignore list');
}

export async function systemOpen(target) {
    const response = await fetch('/api/system/open', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(target)
    });
    if (!response.ok) throw new Error(`Failed to open target`);
}
