namespace HAWindowsCompanion.Core.Models;

/// <summary>
/// Payload for POST /api/mobile_app/registrations.
/// All fields match the Home Assistant mobile_app integration spec.
/// </summary>
public sealed class DeviceRegistration
{
    public required string DeviceId { get; set; }
    public string AppId { get; set; } = "io.homeassistant.companion.windows";
    public string AppName { get; set; } = "HA Windows Companion";
    public required string AppVersion { get; set; }
    public required string DeviceName { get; set; }
    public required string Manufacturer { get; set; }
    public required string Model { get; set; }
    public string OsName { get; set; } = "Windows";
    public required string OsVersion { get; set; }
    public bool SupportsEncryption { get; set; } = false;
    public Dictionary<string, object>? AppData { get; set; }
}
