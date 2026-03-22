using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using HAWindowsCompanion.Core.Interfaces;

namespace HAWindowsCompanion.Infrastructure.Commands;

/// <summary>
/// Handles volume control commands from Home Assistant.
/// Supported commands: volume_set, volume_up, volume_down, volume_mute.
/// </summary>
public sealed class VolumeCommandHandler : ICommandHandler
{
    public string CommandType => "volume_control";

    public Task ExecuteAsync(JsonElement? data)
    {
        if (data == null) return Task.CompletedTask;

        using var enumerator = new MMDeviceEnumerator();
        var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        var volume = device.AudioEndpointVolume;

        if (data.Value.TryGetProperty("action", out var actionProp))
        {
            string action = actionProp.GetString() ?? "";
            switch (action.ToLowerInvariant())
            {
                case "set":
                    if (data.Value.TryGetProperty("level", out var levelProp))
                    {
                        float level = (float)levelProp.GetDouble(); // Expecting 0.0 to 1.0 or 0 to 100
                        if (level > 1.0f) level /= 100.0f;
                        volume.MasterVolumeLevelScalar = Math.Clamp(level, 0.0f, 1.0f);
                    }
                    break;
                case "up":
                    volume.VolumeStepUp();
                    break;
                case "down":
                    volume.VolumeStepDown();
                    break;
                case "mute":
                    if (data.Value.TryGetProperty("mute", out var muteProp))
                        volume.Mute = muteProp.GetBoolean();
                    else
                        volume.Mute = !volume.Mute;
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
