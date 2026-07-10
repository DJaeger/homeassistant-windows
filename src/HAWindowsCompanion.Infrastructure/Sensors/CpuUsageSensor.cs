using System.Diagnostics;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Reports CPU usage percentage using PerformanceCounter.
/// </summary>
public sealed class CpuUsageSensor : ISensorProvider
{
    private readonly PerformanceCounter _cpuCounter;

    public string UniqueId => "cpu_usage";
    public string Name => "CPU Usage";
    public bool IsEnabled { get; set; } = true;

    public CpuUsageSensor()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        // First read always returns 0, so prime it
        _cpuCounter.NextValue();
    }

    public SensorRegistration GetRegistration() => new()
    {
        UniqueId = UniqueId,
        Name = Name,
        Type = "sensor",
        Icon = "mdi:cpu-64-bit",
        UnitOfMeasurement = "%",
        StateClass = "measurement",
        EntityCategory = "diagnostic",
        State = GetCpuUsage()
    };

    public SensorUpdate GetCurrentState() => new()
    {
        UniqueId = UniqueId,
        Type = "sensor",
        Icon = "mdi:cpu-64-bit",
        State = GetCpuUsage()
    };

    private double GetCpuUsage() => Math.Round(_cpuCounter.NextValue(), 1);
}
