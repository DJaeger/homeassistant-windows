using System.Runtime.InteropServices;
using System.Text.Json;
using HAWindowsCompanion.Core.Interfaces;

namespace HAWindowsCompanion.Infrastructure.Commands;

/// <summary>
/// Handles media control commands (Play, Pause, Next, Previous).
/// Simulates media key presses using Win32 keybd_event.
/// </summary>
public sealed class MediaPlayPauseCommandHandler : ICommandHandler
{
    public string CommandType => "media_control";

    private const int VK_MEDIA_NEXT_TRACK = 0xB0;
    private const int VK_MEDIA_PREV_TRACK = 0xB1;
    private const int VK_MEDIA_STOP = 0xB2;
    private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const int KEYEVENTF_EXTENDEDKEY = 1;
    private const int KEYEVENTF_KEYUP = 2;

    public Task ExecuteAsync(JsonElement? data)
    {
        if (data == null) return Task.CompletedTask;

        if (data.Value.TryGetProperty("action", out var actionProp))
        {
            string action = actionProp.GetString() ?? "";
            int keyCode = action.ToLowerInvariant() switch
            {
                "play_pause" => VK_MEDIA_PLAY_PAUSE,
                "next" => VK_MEDIA_NEXT_TRACK,
                "previous" => VK_MEDIA_PREV_TRACK,
                "stop" => VK_MEDIA_STOP,
                _ => 0
            };

            if (keyCode != 0)
            {
                SendKey(keyCode);
            }
        }

        return Task.CompletedTask;
    }

    private static void SendKey(int keyCode)
    {
        keybd_event((byte)keyCode, 0, KEYEVENTF_EXTENDEDKEY, 0);
        keybd_event((byte)keyCode, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
    }

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
}
