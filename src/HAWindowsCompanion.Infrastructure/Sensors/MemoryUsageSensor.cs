using System.Diagnostics;
using System.Runtime.InteropServices;
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] ref MEMORYSTATUSEX lpBuffer);

    private static double GetMemoryUsage()
        {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref memStatus))
        {
            return Math.Round((double)memStatus.dwMemoryLoad, 1);
        }
        return 0;
    }

    private static Dictionary<string, object> GetAttributes()
    {
        var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref memStatus))
        {
        return new Dictionary<string, object>
        {
                ["total_memory_gb"] = Math.Round(memStatus.ullTotalPhys / 1073741824.0, 2)
        };
    }
        return new Dictionary<string, object>();
    }
}
