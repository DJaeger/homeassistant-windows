using System.Net.Http.Headers;
using System.Text.Json;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Api;

public sealed class HomeZoneService : IHomeZoneService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly ICredentialStore _credentialStore;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHttpClientFactory _httpClientFactory;

    private HomeZoneInfo? _cachedHomeZone;
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public HomeZoneService(
        ICredentialStore credentialStore,
        IAuthenticationService authenticationService,
        IHttpClientFactory httpClientFactory)
    {
        _credentialStore = credentialStore;
        _authenticationService = authenticationService;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HomeZoneInfo?> GetHomeZoneAsync()
    {
        if (_cachedHomeZone is not null && DateTimeOffset.UtcNow - _cachedAt < CacheDuration)
        {
            return _cachedHomeZone;
        }

        try
        {
            var serverInfo = await _credentialStore.LoadServerInfoAsync();
            if (serverInfo is null || string.IsNullOrWhiteSpace(serverInfo.InstanceUrl))
            {
                return null;
            }

            var accessToken = await _authenticationService.GetValidAccessTokenAsync();
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.GetAsync($"{serverInfo.InstanceUrl.TrimEnd('/')}/api/states/zone.home");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("attributes", out var attributes))
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
        catch
        {
            return null;
        }
    }
}
