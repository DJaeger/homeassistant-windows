using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Communicates with the Home Assistant mobile_app integration.
/// </summary>
public interface IHomeAssistantClient
{
    /// <summary>
    /// Registers this device with Home Assistant.
    /// POST /api/mobile_app/registrations (authenticated).
    /// </summary>
    Task<HaServerInfo> RegisterDeviceAsync(string instanceUrl, string accessToken, DeviceRegistration registration);

    /// <summary>
    /// Sends a generic webhook request.
    /// POST {webhookUrl} (unauthenticated).
    /// </summary>
    Task<T?> SendWebhookAsync<T>(HaServerInfo server, WebhookRequest request);

    /// <summary>
    /// Registers a single sensor via webhook.
    /// </summary>
    Task<bool> RegisterSensorAsync(HaServerInfo server, SensorRegistration sensor);

    /// <summary>
    /// Updates multiple sensor states at once via webhook.
    /// Returns a dictionary of unique_id → success/error.
    /// </summary>
    Task<Dictionary<string, SensorUpdateResult>> UpdateSensorsAsync(HaServerInfo server, IEnumerable<SensorUpdate> updates);

    /// <summary>
    /// Gets HA config including entity disabled states.
    /// </summary>
    Task<HaConfig?> GetConfigAsync(HaServerInfo server);

    /// <summary>
    /// Updates the device registration.
    /// </summary>
    Task UpdateRegistrationAsync(HaServerInfo server, DeviceRegistration registration);
}

public sealed class SensorUpdateResult
{
    public bool Success { get; set; }
    public bool IsDisabled { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class HaConfig
{
    public Dictionary<string, EntityConfig>? Entities { get; set; }
}

public sealed class EntityConfig
{
    public bool Disabled { get; set; }
}
