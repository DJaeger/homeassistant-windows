using System.Runtime.InteropServices;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Reports the system idle time (time since last user input) in seconds.
/// </summary>
public sealed class SystemIdleTimeSensor : ISensorProvider
{
    public string UniqueId => "system_idle_time";
    public string Name => "System Idle Time";
    public bool IsEnabled { get; set; } = true;

    public SensorRegistration GetRegistration() => new()
    {
        UniqueId = UniqueId,
        Name = Name,
        Type = "sensor",
        DeviceClass = "duration",
        Icon = "mdi:timer-sand",
        UnitOfMeasurement = "s",
        StateClass = "measurement",
        State = GetIdleTimeSeconds()
    };

    public SensorUpdate GetCurrentState() => new()
    {
        UniqueId = UniqueId,
        Type = "sensor",
        Icon = "mdi:timer-sand",
        State = GetIdleTimeSeconds()
    };

    private static long GetIdleTimeSeconds()
    {
        var lastInput = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref lastInput))
            return 0;

        var idleMs = (long)Environment.TickCount64 - lastInput.dwTime;
        return Math.Max(0, idleMs / 1000);
    }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
}
