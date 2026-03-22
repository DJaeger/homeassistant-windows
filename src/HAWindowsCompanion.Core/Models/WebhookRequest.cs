using System.Text.Json.Serialization;

namespace HAWindowsCompanion.Core.Models;

/// <summary>
/// Generic webhook request envelope as defined by the HA mobile_app API.
/// </summary>
public sealed class WebhookRequest
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

/// <summary>
/// Generic webhook response.
/// </summary>
public sealed class WebhookResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("result")]
    public T? Result { get; set; }

    [JsonPropertyName("error")]
    public WebhookError? Error { get; set; }
}

public sealed class WebhookError
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
