using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Api;

/// <summary>
/// Implements communication with the Home Assistant REST API and webhook endpoints.
/// Handles device registration, sensor management, and config retrieval via the mobile_app integration.
/// </summary>
public sealed class HomeAssistantApiClient : IHomeAssistantClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeAssistantApiClient> _logger;

    public HomeAssistantApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<HomeAssistantApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HaServerInfo> RegisterDeviceAsync(
        string instanceUrl, string accessToken, DeviceRegistration registration)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var json = JsonSerializer.Serialize(registration, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Registering device with {InstanceUrl}", instanceUrl);

        var response = await client.PostAsync(
            $"{instanceUrl.TrimEnd('/')}/api/mobile_app/registrations", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegistrationResponse>(responseJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to parse registration response");

        var serverInfo = new HaServerInfo
        {
            InstanceUrl = instanceUrl.TrimEnd('/'),
            CloudhookUrl = result.CloudhookUrl,
            RemoteUiUrl = result.RemoteUiUrl,
            WebhookId = result.WebhookId,
            Secret = result.Secret
        };

        _logger.LogInformation("Device registered successfully, webhook_id: {WebhookId}", result.WebhookId);
        return serverInfo;
    }

    public async Task<T?> SendWebhookAsync<T>(HaServerInfo server, WebhookRequest request)
    {
        var client = _httpClientFactory.CreateClient();
        var webhookUrl = server.GetWebhookUrl();

        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending webhook {Type} to {Url}", request.Type, webhookUrl);

        var response = await client.PostAsync(webhookUrl, content);

        if (response.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            _logger.LogWarning("Received 410 Gone — device registration has been deleted");
            throw new DeviceRegistrationDeletedException();
        }

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(responseJson))
            return default;

        return JsonSerializer.Deserialize<T>(responseJson, JsonOptions);
    }

    public async Task<bool> RegisterSensorAsync(HaServerInfo server, SensorRegistration sensor)
    {
        var request = new WebhookRequest
        {
            Type = "register_sensor",
            Data = sensor
        };

        try
        {
            await SendWebhookAsync<object>(server, request);
            _logger.LogInformation("Sensor registered: {UniqueId} ({Name})", sensor.UniqueId, sensor.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register sensor: {UniqueId}", sensor.UniqueId);
            return false;
        }
    }

    public async Task<Dictionary<string, SensorUpdateResult>> UpdateSensorsAsync(
        HaServerInfo server, IEnumerable<SensorUpdate> updates)
    {
        var request = new WebhookRequest
        {
            Type = "update_sensor_states",
            Data = updates.ToArray()
        };

        var result = await SendWebhookAsync<Dictionary<string, JsonElement>>(server, request);
        var parsed = new Dictionary<string, SensorUpdateResult>();

        if (result is null) return parsed;

        foreach (var (uniqueId, element) in result)
        {
            var updateResult = new SensorUpdateResult();

            if (element.TryGetProperty("success", out var successProp))
                updateResult.Success = successProp.GetBoolean();

            if (element.TryGetProperty("is_disabled", out var disabledProp))
                updateResult.IsDisabled = disabledProp.GetBoolean();

            if (element.TryGetProperty("error", out var errorProp))
            {
                if (errorProp.TryGetProperty("code", out var codeProp))
                    updateResult.ErrorCode = codeProp.GetString();
                if (errorProp.TryGetProperty("message", out var msgProp))
                    updateResult.ErrorMessage = msgProp.GetString();
            }

            parsed[uniqueId] = updateResult;
        }

        return parsed;
    }

    public async Task<HaConfig?> GetConfigAsync(HaServerInfo server)
    {
        var request = new WebhookRequest { Type = "get_config" };
        return await SendWebhookAsync<HaConfig>(server, request);
    }

    public async Task UpdateRegistrationAsync(HaServerInfo server, DeviceRegistration registration)
    {
        var request = new WebhookRequest
        {
            Type = "update_registration",
            Data = registration
        };

        await SendWebhookAsync<object>(server, request);
        _logger.LogInformation("Device registration updated");
    }

    private sealed class RegistrationResponse
    {
        [JsonPropertyName("cloudhook_url")]
        public string? CloudhookUrl { get; set; }

        [JsonPropertyName("remote_ui_url")]
        public string? RemoteUiUrl { get; set; }

        [JsonPropertyName("secret")]
        public string? Secret { get; set; }

        [JsonPropertyName("webhook_id")]
        public string? WebhookId { get; set; }
    }
}

/// <summary>
/// Thrown when HA responds with 410 Gone, meaning the device registration was deleted.
/// </summary>
public sealed class DeviceRegistrationDeletedException : Exception
{
    public DeviceRegistrationDeletedException()
        : base("The device registration has been deleted from Home Assistant. Re-registration is required.")
    { }
}
