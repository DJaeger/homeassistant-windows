using System.Text.Json;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;
using HAWindowsCompanion.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HAWindowsCompanion.Infrastructure.Api;

public sealed class ZonesService : IZonesService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IHomeAssistantClient _haClient;
    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<ZonesService> _logger;

    private List<Zone> _cachedZones = [];
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    public ZonesService(
        IHomeAssistantClient haClient,
        ICredentialStore credentialStore,
        ILogger<ZonesService> logger)
    {
        _haClient = haClient;
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public async Task<List<Zone>?> GetZonesAsync()
    {
        if (_cachedZones.Count > 0 && DateTimeOffset.UtcNow - _cachedAt < CacheDuration)
        {
            return _cachedZones;
        }

        try
        {
            var server = await _credentialStore.LoadServerInfoAsync();
            if (server == null || string.IsNullOrEmpty(server.WebhookId))
            {
                _logger.LogError("ZonesService: Application is marked configured but no server info/webhook ID found.");
                return null;
            }

            var request = new WebhookRequest { Type = "get_zones" };
            var result = await _haClient.SendWebhookAsync<List<JsonElement>>(server, request);
            _cachedZones = [];

            if (result == null) return _cachedZones;

            foreach (var element in result)
            {
                if (element.TryGetProperty("entity_id", out var entityId)
                    && element.TryGetProperty("attributes", out var attributes))
                {
                    if (attributes.TryGetProperty("friendly_name", out var name)
                        && attributes.TryGetProperty("latitude", out var latitude)
                        && attributes.TryGetProperty("longitude", out var longitude)
                        && attributes.TryGetProperty("radius", out var radius)
                        && attributes.TryGetProperty("passive", out var passive))
                    {
                        if (passive.GetBoolean() == false)
                        {
                            _cachedZones.Add(new Zone
                            {
                                EntityId = entityId.GetString() ?? "zone.unknown",
                                Name = name.GetString() ?? "unknown",
                                Latitude = latitude.GetDouble(),
                                Longitude = longitude.GetDouble(),
                                RadiusMeters = radius.GetDouble()
                            });
                        }
                    }
                }
            }

            _cachedAt = DateTimeOffset.UtcNow;

            return _cachedZones;

        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Zone>> GetZonesForLocationAsync(Location location)
    {

        var zones = await GetZonesAsync();
        if (zones == null || zones.Count == 0)
        {
            return [];
        }

        List<Zone> zonesInRadius = [];

        foreach (var zone in zones)
        {
            if (LocationHelpers.ZoneContainsLocationWithAccuracy(zone, location))
            {
                zonesInRadius.Add(zone);
            }
        }

        // Smallest zone (radius) first; when two zones share the same radius, prefer the one
        // whose center is closest to the current location to break the tie deterministically.
        var sortedZones = zonesInRadius.OrderBy(
            zone => zone.RadiusMeters
        ).ThenBy(
            zone => LocationHelpers.CalculateDistanceMeters(
                location.Latitude,
                location.Longitude,
                zone.Latitude,
                zone.Longitude
            )
        ).ToList();

        return sortedZones;
    }

}
