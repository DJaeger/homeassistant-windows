using System.Text.Json.Serialization;

namespace HAWindowsCompanion.Core.Models;

/// <summary>
/// OAuth2 token response from Home Assistant.
/// </summary>
public sealed class TokenInfo
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Absolute UTC time when the access token expires.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// Whether this represents a long-lived access token (no refresh token).
    /// </summary>
    [JsonIgnore]
    public bool IsLongLived => string.IsNullOrEmpty(RefreshToken);
}
