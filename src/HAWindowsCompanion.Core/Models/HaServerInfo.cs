namespace HAWindowsCompanion.Core.Models;

/// <summary>
/// Represents connection and registration info for a Home Assistant instance.
/// </summary>
public sealed class HaServerInfo
{
    public required string InstanceUrl { get; set; }
    public string? CloudhookUrl { get; set; }
    public string? RemoteUiUrl { get; set; }
    public string? WebhookId { get; set; }
    public string? Secret { get; set; }

    /// <summary>
    /// Resolves the best webhook URL using the fallback chain:
    /// CloudhookUrl → RemoteUiUrl/api/webhook/{id} → InstanceUrl/api/webhook/{id}
    /// </summary>
    public string GetWebhookUrl()
    {
        if (!string.IsNullOrEmpty(CloudhookUrl))
            return CloudhookUrl;

        var baseUrl = !string.IsNullOrEmpty(RemoteUiUrl) ? RemoteUiUrl : InstanceUrl;
        return $"{baseUrl.TrimEnd('/')}/api/webhook/{WebhookId}";
    }
}
