using System.Runtime.InteropServices;
using System.Text;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Reports the title of the currently focused (foreground) window.
/// </summary>
public sealed class ActiveWindowSensor : ISensorProvider
{
    public string UniqueId => "active_window";
    public string Name => "Active Window";
    public bool IsEnabled { get; set; } = true;

    public SensorRegistration GetRegistration() => new()
    {
        UniqueId = UniqueId,
        Name = Name,
        Type = "sensor",
        Icon = "mdi:window-maximize",
        State = GetActiveWindowTitle()
    };

    public SensorUpdate GetCurrentState() => new()
    {
        UniqueId = UniqueId,
        Type = "sensor",
        Icon = "mdi:window-maximize",
        State = GetActiveWindowTitle()
    };

    private static string GetActiveWindowTitle()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return "Unknown";

        var sb = new StringBuilder(256);
        return GetWindowText(hwnd, sb, sb.Capacity) > 0
            ? sb.ToString()
            : "Unknown";
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
}
