using System;

namespace CheckModsExtended.Utils;

/// <summary>
/// Builds the GitHub "new issue" URL for reporting unhandled crashes.
/// </summary>
internal static class CrashReportUrl
{
    private const string BaseUrl =
        "https://github.com/TerribleTurtle/CheckModsExtended/issues/new?template=bug_report.yml";
    private const int MaxUrlLength = 7000;

    /// <summary>
    /// Builds the report URL for the given exception.
    /// </summary>
    /// <param name="ex">The exception to report.</param>
    /// <param name="version">The current application version.</param>
    public static string Build(Exception ex, string version)
    {
        var title = Uri.EscapeDataString($"Crash: {ex.GetType().Name}");
        var description = Uri.EscapeDataString($"An unhandled exception occurred:\n\n{ex.Message}");
        var logs = Uri.EscapeDataString($"```text\n{ex.StackTrace}\n```");
        var versionEncoded = Uri.EscapeDataString(version);

        var url =
            $"{BaseUrl}&title={title}&description={description}&logs={logs}&check-mods-extended-version={versionEncoded}";

        if (url.Length <= MaxUrlLength)
        {
            return url;
        }

        return BaseUrl;
    }
}
