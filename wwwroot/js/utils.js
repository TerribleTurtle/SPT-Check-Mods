/**
 * Escapes HTML characters in a string to prevent XSS.
 * @example escapeHtml('<div>"Hello" & ''World''</div>') // returns '&lt;div&gt;&quot;Hello&quot; &amp; &#039;World&#039;&lt;/div&gt;'
 */
export function escapeHtml(unsafe) {
    if (unsafe == null) return '';
    return unsafe
         .toString()
         .replace(/&/g, "&amp;")
         .replace(/</g, "&lt;")
         .replace(/>/g, "&gt;")
         .replace(/"/g, "&quot;")
         .replace(/'/g, "&#039;");
}

/**
 * Logs a message to the UI console drawer.
 * @param {string} message - The message to log.
 * @param {string} [type=''] - Optional log type (e.g., 'error', 'success', 'warn').
 * @example logToConsole('Scan complete', 'success')
 */
export function logToConsole(message, type = '') {
    const consoleLogs = document.getElementById('console-logs');
    if (!consoleLogs) return;
    const div = document.createElement('div');
    div.className = 'log-line';
    if (type === 'error') div.classList.add('log-error');
    if (type === 'success') div.classList.add('log-success');
    if (type === 'warn') div.classList.add('log-warn');
    
    div.textContent = message;
    consoleLogs.appendChild(div);
    consoleLogs.scrollTop = consoleLogs.scrollHeight;
}
