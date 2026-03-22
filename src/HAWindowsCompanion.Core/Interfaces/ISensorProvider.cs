using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Provides sensor data from Windows system APIs.
/// Each implementation represents one sensor type (CPU, RAM, Battery, etc.).
/// </summary>
public interface ISensorProvider
{
    /// <summary>Unique identifier for this sensor (e.g., "cpu_usage").</summary>
    string UniqueId { get; }

    /// <summary>Human-readable sensor name shown in Home Assistant.</summary>
    string Name { get; }

    /// <summary>Whether this sensor is currently enabled by the user.</summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Returns the full registration payload for this sensor.
    /// Called once on first registration or when re-registration is needed.
    /// </summary>
    SensorRegistration GetRegistration();

    /// <summary>
    /// Returns the current state update payload.
    /// Called periodically by the SensorManager.
    /// </summary>
    SensorUpdate GetCurrentState();
}
