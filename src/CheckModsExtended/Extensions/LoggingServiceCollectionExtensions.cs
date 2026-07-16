using System.IO;
using CheckModsExtended.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace CheckModsExtended.Extensions;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLogging(this IServiceCollection services, IConfiguration configuration, RuntimeConfig runtimeConfig)
    {
        services.AddLogging(builder =>
        {
            // Suppress verbose HttpClient logging and Polly execution attempt logs.
            builder.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            builder.AddFilter("Polly", LogLevel.Warning);

            var loggingOptions =
                configuration.GetSection("LoggingOptions").Get<LoggingOptions>() ?? new LoggingOptions();

            var appPaths = configuration.GetSection("AppPaths").Get<AppPaths>() ?? new AppPaths();

            if (runtimeConfig.IsVerbose)
            {
                loggingOptions.MinimumLogLevel = LogLevel.Debug;
            }

            if (loggingOptions.EnableFileLogging)
            {
                string logPath = loggingOptions.LogFilePath;
                if (!string.IsNullOrEmpty(logPath) && !Path.IsPathRooted(logPath))
                {
                    logPath = Path.Combine(appPaths.AppDataDirectory, "logs", logPath);
                }
                LogEventLevel serilogLevel = loggingOptions.MinimumLogLevel switch
                {
                    LogLevel.Trace => LogEventLevel.Verbose,
                    LogLevel.Debug => LogEventLevel.Debug,
                    LogLevel.Information => LogEventLevel.Information,
                    LogLevel.Warning => LogEventLevel.Warning,
                    LogLevel.Error => LogEventLevel.Error,
                    LogLevel.Critical => LogEventLevel.Fatal,
                    _ => LogEventLevel.Information,
                };

                var serilogLogger = new Serilog.LoggerConfiguration()
                    .MinimumLevel.Is(serilogLevel)
                    .WriteTo.File(
                        logPath,
                        fileSizeLimitBytes: loggingOptions.MaxFileSizeBytes,
                        rollOnFileSizeLimit: true,
                        retainedFileCountLimit: loggingOptions.RetainedFileCount,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                    )
                    .CreateLogger();

                builder.AddSerilog(serilogLogger, dispose: true);
            }
        });

        return services;
    }
}
