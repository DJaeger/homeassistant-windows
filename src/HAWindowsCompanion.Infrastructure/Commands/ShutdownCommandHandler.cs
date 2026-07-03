using System.Diagnostics;
using System.Text.Json;
using HAWindowsCompanion.Core.Interfaces;

namespace HAWindowsCompanion.Infrastructure.Commands;

/// <summary>
/// Handles shutdown and restart commands.
/// Uses 'shutdown.exe' for reliable cross-version execution.
/// </summary>
public sealed class ShutdownCommandHandler : ICommandHandler
{
    public string CommandType => "system_control";

    public Task ExecuteAsync(JsonElement? data)
    {
        if (data == null) return Task.CompletedTask;

        if (data.Value.TryGetProperty("action", out var actionProp))
        {
            string action = actionProp.GetString() ?? "";
            switch (action.ToLowerInvariant())
            {
                case "shutdown":
                    Process.Start("shutdown.exe", "/s /t 0 /f");
                    break;
                case "restart":
                    Process.Start("shutdown.exe", "/r /t 0 /f");
                    break;
                case "suspend":
                    // SetSuspendState requires PowrProf.dll or using 'rundll32.exe powrprof.dll,SetSuspendState 0,1,0'
                    Process.Start("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0");
                    break;
            }
        }

        return Task.CompletedTask;
    }
}
