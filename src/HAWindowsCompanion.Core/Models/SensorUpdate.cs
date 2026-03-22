using System.Text.Json.Serialization;

namespace HAWindowsCompanion.Core.Models;

/// <summary>
/// Represents a sensor state update sent via the update_sensor_states webhook.
/// </summary>
public sealed class SensorUpdate
{
    [JsonPropertyName("unique_id")]
    public required string UniqueId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "sensor";

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("state")]
    public object? State { get; set; }

    [JsonPropertyName("attributes")]
    public Dictionary<string, object>? Attributes { get; set; }
}
