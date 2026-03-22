using System.Diagnostics;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Reports memory (RAM) usage percentage.
/// </summary>
public sealed class MemoryUsageSensor : ISensorProvider
{
    public string UniqueId => "memory_usage";
    public string Name => "Memory Usage";
    public bool IsEnabled { get; set; } = true;

    public SensorRegistration GetRegistration() => new()
    {
        UniqueId = UniqueId,
        Name = Name,
        Type = "sensor",
        Icon = "mdi:memory",
        UnitOfMeasurement = "%",
        StateClass = "measurement",
        EntityCategory = "diagnostic",
        State = GetMemoryUsage()
    };

    public SensorUpdate GetCurrentState() => new()
    {
        UniqueId = UniqueId,
        Type = "sensor",
        Icon = "mdi:memory",
        State = GetMemoryUsage(),
        Attributes = GetAttributes()
    };

    private static double GetMemoryUsage()
    {
        var info = GC.GetGCMemoryInfo();
        var totalMemory = info.TotalAvailableMemoryBytes;
        var usedMemory = totalMemory - info.HighMemoryLoadThresholdBytes;

        // Use performance counter for accurate system-wide memory
        try
        {
            using var counter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            return Math.Round(counter.NextValue(), 1);
        }
        catch
        {
            // Fallback: estimate from GC info
            var process = Process.GetCurrentProcess();
            return totalMemory > 0
                ? Math.Round((1.0 - (double)info.HighMemoryLoadThresholdBytes / totalMemory) * 100, 1)
                : 0;
        }
    }

    private static Dictionary<string, object> GetAttributes()
    {
        var info = GC.GetGCMemoryInfo();
        return new Dictionary<string, object>
        {
            ["total_memory_gb"] = Math.Round(info.TotalAvailableMemoryBytes / 1073741824.0, 2)
        };
    }
}
