using System.Text;
using System.Text.Json;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;
using Microsoft.Extensions.Logging;

namespace HAWindowsCompanion.Infrastructure.Api;

public sealed class HomeZoneService : IHomeZoneService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IHomeAssistantClient _haClient;
    private readonly ICredentialStore _credentialStore;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HomeAssistantApiClient> _logger;

    private HomeZoneInfo? _cachedHomeZone;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public HomeZoneService(
        IHomeAssistantClient haClient,
        ICredentialStore credentialStore,
        IAuthenticationService authenticationService,
        IHttpClientFactory httpClientFactory,
        ILogger<HomeAssistantApiClient> logger)
    {
        _haClient = haClient;
        _credentialStore = credentialStore;
        _authenticationService = authenticationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<HomeZoneInfo?> GetHomeZoneAsync()
    {
        if (_cachedHomeZone is not null && DateTimeOffset.UtcNow - _cachedAt < CacheDuration)
        {
            return _cachedHomeZone;
        }

        try
        {
            var server = await _credentialStore.LoadServerInfoAsync();
            if (server == null || string.IsNullOrEmpty(server.WebhookId))
            {
                _logger.LogError("HomeZoneService: Application is marked configured but no server info/webhook ID found.");
                return null;
            }

            var request = new WebhookRequest { Type = "get_zones" };
            var result = await _haClient.SendWebhookAsync<List<JsonElement>>(server, request);

            if (result == null) return null;

            foreach (var element in result)

                if (element.TryGetProperty("entity_id", out var entityId))
                {
                    if (entityId.GetString() != "zone.home")
                    {
                        continue;
                    }

                    if (!element.TryGetProperty("attributes", out var attributes))
                    {
                        return null;
                    }

                    if (!attributes.TryGetProperty("latitude", out var latitude)
                        || !attributes.TryGetProperty("longitude", out var longitude)
                        || !attributes.TryGetProperty("radius", out var radius))
                    {
                        return null;
                    }

                    _cachedHomeZone = new HomeZoneInfo
                    {
                        Latitude = latitude.GetDouble(),
                        Longitude = longitude.GetDouble(),
                        RadiusMeters = radius.GetDouble()
                    };

                    _cachedAt = DateTimeOffset.UtcNow;

                    return _cachedHomeZone;
                }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
