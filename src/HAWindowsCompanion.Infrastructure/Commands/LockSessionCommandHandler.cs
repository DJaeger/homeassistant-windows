using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using HAWindowsCompanion.Core.Interfaces;

namespace HAWindowsCompanion.Infrastructure.Commands;

/// <summary>
/// Handles session lock commands.
/// Executing 'LockWorkStation' from user32.dll.
/// </summary>
public sealed class LockSessionCommandHandler : ICommandHandler
{
    public string CommandType => "lock_session";

    public Task ExecuteAsync(JsonElement? data)
    {
        LockWorkStation();
        return Task.CompletedTask;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();
}
