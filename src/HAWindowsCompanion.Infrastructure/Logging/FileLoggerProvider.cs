using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HAWindowsCompanion.Infrastructure.Logging;

[ProviderAlias("File")]
public sealed class FileLoggerProvider(
    FileLoggerOptions _options
) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _options));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
