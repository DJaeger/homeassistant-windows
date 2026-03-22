using System.Text.Json.Serialization;

namespace HAWindowsCompanion.Core.Models;

/// <summary>
/// Represents a sensor to be registered with Home Assistant via the register_sensor webhook.
/// </summary>
public sealed class SensorRegistration
{
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "sensor";

    [JsonPropertyName("device_class")]
    public string? DeviceClass { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("unit_of_measurement")]
    public string? UnitOfMeasurement { get; set; }

    [JsonPropertyName("state_class")]
    public string? StateClass { get; set; }

    [JsonPropertyName("entity_category")]
    public string? EntityCategory { get; set; }

    [JsonPropertyName("state")]
    public object? State { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, object>? Attributes { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; } = false;
}
