using System;
using System.Runtime.InteropServices;

namespace CheckModsExtended.Utils;

/// <summary>
/// Helper to enable Virtual Terminal Processing on Windows to restore ANSI colors
/// when the application is trimmed (since Trimming breaks Spectre.Console's detection).
/// </summary>
public static class WindowsConsoleHelper
{
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    /// <summary>
    /// Attempts to enable VT processing on Windows so ANSI escape sequences render properly.
    /// Does nothing on non-Windows platforms.
    /// </summary>
    public static void TryEnableVirtualTerminalProcessing()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        try
        {
            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
                SetConsoleMode(iStdOut, outConsoleMode);
            }
        }
        catch
        {
            // Ignore any P/Invoke exceptions just in case.
        }
    }
}
