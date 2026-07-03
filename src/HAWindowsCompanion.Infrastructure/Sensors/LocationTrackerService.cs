using Windows.Devices.Geolocation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;
using HAWindowsCompanion.Core.Utilities;

namespace HAWindowsCompanion.Infrastructure.Sensors;

public sealed class LocationTrackerService : BackgroundService
{
    public sealed class LastKnownLocationStatus
    {
        public string LocationName { get; init; } = "unknown";
        public Dictionary<string, object> Attributes { get; init; } = new();
    }

    private static readonly TimeSpan DefaultUpdateInterval = TimeSpan.FromSeconds(60);

    private readonly Geolocator _geolocator;
    private readonly IHomeAssistantClient _haClient;
    private readonly ICredentialStore _credentialStore;
    private readonly ISettingsService _settingsService;
    private readonly IHomeZoneService _homeZoneService;
    private readonly ILogger<LocationTrackerService> _logger;

    private readonly object _statusLock = new();
    private string _lastLocationName = "unknown";
    private Dictionary<string, object> _lastAttributes = new();

    public LocationTrackerService(
        IHomeAssistantClient haClient,
        ICredentialStore credentialStore,
        ISettingsService settingsService,
        IHomeZoneService homeZoneService,
        ILogger<LocationTrackerService> logger)
    {
        _haClient = haClient;
        _credentialStore = credentialStore;
        _settingsService = settingsService;
        _homeZoneService = homeZoneService;
        _logger = logger;
        _geolocator = new Geolocator { DesiredAccuracyInMeters = 50 };
    }

    public LastKnownLocationStatus CurrentStatus
    {
        get
        {
            lock (_statusLock)
            {
                return new LastKnownLocationStatus
                {
                    LocationName = _lastLocationName,
                    Attributes = new Dictionary<string, object>(_lastAttributes)
                };
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LocationTracker service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var isConfigured = await _settingsService.GetAsync<bool>("IsConfigured");
            if (isConfigured) break;
            await Task.Delay(5000, stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested) return;

        var server = await _credentialStore.LoadServerInfoAsync();
        if (server == null || string.IsNullOrEmpty(server.WebhookId))
        {
            _logger.LogError("LocationTracker: Application is marked configured but no server info/webhook ID found.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateLocationAsync(server, stoppingToken);

                var intervalSeconds = await _settingsService.GetAsync<int>("SensorUpdateIntervalSeconds");
                var interval = intervalSeconds > 0 ? TimeSpan.FromSeconds(intervalSeconds) : DefaultUpdateInterval;
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic location update");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task UpdateLocationAsync(HaServerInfo server, CancellationToken cancellationToken)
    {
        try
        {
            var position = await _geolocator
                .GetGeopositionAsync(TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(10))
                .AsTask(cancellationToken);

            var coordinate = position.Coordinate;
            var pos = coordinate.Point.Position;
            var locationName = await ResolveLocationNameAsync(pos.Latitude, pos.Longitude);

            var payload = new Dictionary<string, object>
            {
                ["gps"] = new[] { pos.Latitude, pos.Longitude },
                ["gps_accuracy"] = Math.Max(1, (int)Math.Round(coordinate.Accuracy))
            };

            if (!string.IsNullOrWhiteSpace(locationName))
            {
                payload["location_name"] = locationName;
            }

            if (!double.IsNaN(pos.Altitude) && !double.IsInfinity(pos.Altitude))
            {
                payload["altitude"] = (int)Math.Round(pos.Altitude);
            }

            if (coordinate.AltitudeAccuracy.HasValue
                && !double.IsNaN(coordinate.AltitudeAccuracy.Value)
                && !double.IsInfinity(coordinate.AltitudeAccuracy.Value)
                && coordinate.AltitudeAccuracy.Value >= 0)
            {
                payload["vertical_accuracy"] = (int)Math.Round(coordinate.AltitudeAccuracy.Value);
            }

            if (coordinate.Heading.HasValue
                && !double.IsNaN(coordinate.Heading.Value)
                && !double.IsInfinity(coordinate.Heading.Value)
                && coordinate.Heading.Value >= 0)
            {
                payload["course"] = (int)Math.Round(coordinate.Heading.Value);
            }

            if (coordinate.Speed.HasValue
                && !double.IsNaN(coordinate.Speed.Value)
                && !double.IsInfinity(coordinate.Speed.Value)
                && coordinate.Speed.Value >= 0)
            {
                payload["speed"] = (int)Math.Round(coordinate.Speed.Value);
            }

            lock (_statusLock)
            {
                _lastLocationName = locationName;
                _lastAttributes = new Dictionary<string, object>(payload);
            }

            await _haClient.SendWebhookAsync<object>(server, new WebhookRequest
            {
                Type = "update_location",
                Data = payload
            });
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location unavailable. Reporting unavailable state to Home Assistant.");

            lock (_statusLock)
            {
                _lastLocationName = "unavailable";
                _lastAttributes = new Dictionary<string, object>
                {
                    ["location_name"] = "unavailable"
                };
            }

            await _haClient.SendWebhookAsync<object>(server, new WebhookRequest
            {
                Type = "update_location",
                Data = new Dictionary<string, object>
                {
                    ["location_name"] = "unavailable"
                }
            });
        }
    }

    private async Task<string> ResolveLocationNameAsync(double latitude, double longitude)
    {
        var homeZone = await _homeZoneService.GetHomeZoneAsync();
        if (homeZone is null)
        {
            return "unknown";
        }

        var distance = GeoDistanceCalculator.CalculateDistanceMeters(
            latitude,
            longitude,
            homeZone.Latitude,
            homeZone.Longitude);

        return distance <= homeZone.RadiusMeters ? "home" : "not_home";
    }
}
