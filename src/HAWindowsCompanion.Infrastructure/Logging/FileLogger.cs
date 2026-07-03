using Microsoft.Extensions.Logging;

namespace HAWindowsCompanion.Infrastructure.Logging;

internal sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly FileLoggerOptions _options;
    private readonly object _lock = new();
    private string? _currentLogFile;
    private DateTime _currentLogDate;

    public FileLogger(string categoryName, FileLoggerOptions options)
    {
        _categoryName = categoryName;
        _options = options;
        EnsureLogDirectory();
        CleanupOldLogs();
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _options.MinimumLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);

        // Format: [YYYY-MM-DD HH:mm:ss] [LogLevel] Category: Message
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] {_categoryName}: {message}";

        if (exception != null)
        {
            logEntry += Environment.NewLine + exception.ToString();
        }

        lock (_lock)
        {
            try
            {
                EnsureCurrentLogFile();
                File.AppendAllText(_currentLogFile!, logEntry + Environment.NewLine);
            }
            catch
            {
                // Fail silently to avoid breaking the app
            }
        }
    }

    private void EnsureLogDirectory()
    {
        if (!Directory.Exists(_options.LogDirectory))
        {
            Directory.CreateDirectory(_options.LogDirectory);
        }
    }

    private void EnsureCurrentLogFile()
    {
        var today = DateTime.Now.Date;
        if (_currentLogFile == null || _currentLogDate != today)
        {
            _currentLogDate = today;
            _currentLogFile = Path.Combine(
                _options.LogDirectory,
                $"app-{today:yyyy-MM-dd}.log");
        }
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
}
