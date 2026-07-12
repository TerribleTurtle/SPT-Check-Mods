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
