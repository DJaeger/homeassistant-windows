using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HAWindowsCompanion.Infrastructure.Logging;

[ProviderAlias("File")]
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly FileLoggerOptions _options;
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public FileLoggerProvider(FileLoggerOptions options)
    {
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
