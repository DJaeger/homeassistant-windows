using System.Text.Json;

namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Handles a specific command type received from Home Assistant.
/// </summary>
public interface ICommandHandler
{
    /// <summary>The command type this handler responds to (e.g., "volume_set").</summary>
    string CommandType { get; }

    /// <summary>
    /// Executes the command with optional JSON data payload.
    /// </summary>
    Task ExecuteAsync(JsonElement? data);
}
