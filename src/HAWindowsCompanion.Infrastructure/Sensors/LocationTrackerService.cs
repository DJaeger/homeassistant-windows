using Windows.Devices.Geolocation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

public sealed class LocationTrackerService : BackgroundService
{
    public sealed class LastKnownLocationStatus
    {
        public string LocationName { get; init; } = "unknown";
        public Dictionary<string, object> Attributes { get; init; } = [];
    }

    private static readonly TimeSpan DefaultUpdateInterval = TimeSpan.FromSeconds(60);

    private readonly Geolocator _geolocator;
    private readonly IHomeAssistantClient _haClient;
    private readonly ICredentialStore _credentialStore;
    private readonly ISettingsService _settingsService;
    private readonly IZonesService _zonesService;
    private readonly ILogger<LocationTrackerService> _logger;

    private readonly object _statusLock = new();
    private string _lastLocationName = "unknown";
    private Dictionary<string, object> _lastAttributes = [];

    public LocationTrackerService(
        IHomeAssistantClient haClient,
        ICredentialStore credentialStore,
        ISettingsService settingsService,
        IZonesService zonesService,
        ILogger<LocationTrackerService> logger)
    {
        _haClient = haClient;
        _credentialStore = credentialStore;
        _settingsService = settingsService;
        _zonesService = zonesService;
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
            Geoposition position = await _geolocator
                .GetGeopositionAsync(TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(10))
                .AsTask(cancellationToken);

            Geocoordinate coordinate = position.Coordinate;
            BasicGeoposition pos = coordinate.Point.Position;
            int accuracy = Math.Max(1, (int)Math.Round(coordinate.Accuracy));
            List<Zone> zones = await _zonesService.GetZonesForLocationAsync(
                new Location
                {
                    Latitude = pos.Latitude,
                    Longitude = pos.Longitude,
                    Accuracy = accuracy
                }
            );

            Dictionary<string,object> payload = new Dictionary<string, object>
            {
                ["gps"] = new[] { pos.Latitude, pos.Longitude },
                ["gps_accuracy"] = accuracy
            };

            payload["in_zones"] = zones.ConvertAll(zone => zone.EntityId.ToString());

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

            await _haClient.SendWebhookAsync<object>(server, new WebhookRequest
            {
                Type = "update_location",
                Data = payload
            });

            payload["in_zones"] = string.Join(", ", zones.ConvertAll(zone => zone.EntityId));
            lock (_statusLock)
            {
                _lastLocationName = zones.FirstOrDefault()?.Name ?? "unknown";
                _lastAttributes = new Dictionary<string, object>(payload);
        }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location unknown. Reporting unknown state to Home Assistant.");

            lock (_statusLock)
            {
                _lastLocationName = "unknown";
                _lastAttributes = new Dictionary<string, object>{};
            }

            await _haClient.SendWebhookAsync<object>(server, new WebhookRequest
            {
                Type = "update_location",
                Data = new Dictionary<string, object>{}
            });
        }
    }
}
