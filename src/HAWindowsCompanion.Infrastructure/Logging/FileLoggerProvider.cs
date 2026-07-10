using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HAWindowsCompanion.Infrastructure.Logging;

[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerOptions _options;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
    private readonly object _sharedLock = new();

    public FileLoggerProvider(FileLoggerOptions options)
    {
        _options = options;
        CleanupOldLogs();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options, _sharedLock));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }

    private void CleanupOldLogs()
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-_options.RetentionDays);
            if (!Directory.Exists(_options.LogDirectory))
                return;

            var logFiles = Directory.GetFiles(_options.LogDirectory, "app-*.log");
            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Fail silently
        }
    }
