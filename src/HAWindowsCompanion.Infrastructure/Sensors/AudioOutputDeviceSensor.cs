using NAudio.CoreAudioApi;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Reports the currently active audio output device name.
/// </summary>
public sealed class AudioOutputDeviceSensor : ISensorProvider
{
    public string UniqueId => "audio_output_device";
    public string Name => "Audio Output Device";
    public bool IsEnabled { get; set; } = true;

    public SensorRegistration GetRegistration() => new()
    {
        UniqueId = UniqueId,
        Name = Name,
        Type = "sensor",
        Icon = "mdi:speaker",
        State = GetCurrentDevice()
    };

    public SensorUpdate GetCurrentState() => new()
    {
        UniqueId = UniqueId,
        Type = "sensor",
        Icon = "mdi:speaker",
        State = GetCurrentDevice(),
        Attributes = GetAttributes()
    };

    private static string GetCurrentDevice()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device.FriendlyName;
        }
        catch
        {
            return "Unknown";
        }
    }

    private static Dictionary<string, object> GetAttributes()
    {
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return new Dictionary<string, object>
            {
                ["volume"] = Math.Round(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100, 0),
                ["muted"] = device.AudioEndpointVolume.Mute
            };
        }
        catch
        {
            return new();
        }
    }
}
