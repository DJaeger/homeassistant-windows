using Microsoft.Extensions.Logging;

namespace HAWindowsCompanion.Infrastructure.Logging;

public sealed class FileLoggerOptions
{
    public string LogDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HAWindowsCompanion",
        "logs");

    public int RetentionDays { get; set; } = 7;
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;
}
