using System.Runtime.InteropServices;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Reports battery level and charging status using Windows SYSTEM_POWER_STATUS.
/// </summary>
public sealed class BatteryStatusSensor : ISensorProvider
{
    public string UniqueId => "battery_level";
    public string Name => "Battery Level";
    public bool IsEnabled { get; set; } = true;

    public SensorRegistration GetRegistration() => new()
    {
        UniqueId = UniqueId,
        Name = Name,
        Type = "sensor",
        DeviceClass = "battery",
        Icon = "mdi:battery",
        UnitOfMeasurement = "%",
        StateClass = "measurement",
        State = GetBatteryLevel(),
        Attributes = GetAttributes()
    };

    public SensorUpdate GetCurrentState() => new()
    {
        UniqueId = UniqueId,
        Type = "sensor",
        Icon = GetBatteryIcon(),
        State = GetBatteryLevel(),
        Attributes = GetAttributes()
    };

    private static int GetBatteryLevel()
    {
        if (!GetSystemPowerStatus(out var status))
            return -1;

        return status.BatteryLifePercent == 255 ? -1 : status.BatteryLifePercent;
    }

    private static Dictionary<string, object> GetAttributes()
    {
        if (!GetSystemPowerStatus(out var status))
            return new() { ["ac_power"] = "unknown" };

        return new Dictionary<string, object>
        {
            ["is_charging"] = (status.BatteryFlag & 8) != 0,
            ["ac_power"] = status.ACLineStatus == 1,
            ["battery_present"] = status.BatteryFlag != 128
        };
    }

    private static string GetBatteryIcon()
    {
        if (!GetSystemPowerStatus(out var status))
            return "mdi:battery-unknown";

        if ((status.BatteryFlag & 8) != 0)
            return "mdi:battery-charging";

        return status.BatteryLifePercent switch
        {
            >= 90 => "mdi:battery",
            >= 70 => "mdi:battery-70",
            >= 50 => "mdi:battery-50",
            >= 30 => "mdi:battery-30",
            >= 10 => "mdi:battery-10",
            _ => "mdi:battery-alert"
        };
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }
}
