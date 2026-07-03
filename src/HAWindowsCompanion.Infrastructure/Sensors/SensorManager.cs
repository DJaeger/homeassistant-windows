using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Orchestrates all registered ISensorProvider instances.
/// Handles initial registration with Home Assistant and periodic state updates.
/// </summary>
public sealed class SensorManager : BackgroundService
{
    private readonly IEnumerable<ISensorProvider> _sensors;
    private readonly IHomeAssistantClient _haClient;
    private readonly ICredentialStore _credentialStore;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SensorManager> _logger;

    private static readonly TimeSpan DefaultUpdateInterval = TimeSpan.FromSeconds(60);

    public SensorManager(
        IEnumerable<ISensorProvider> sensors,
        IHomeAssistantClient haClient,
        ICredentialStore credentialStore,
        ISettingsService settingsService,
        ILogger<SensorManager> logger)
    {
        _sensors = sensors;
        _haClient = haClient;
        _credentialStore = credentialStore;
        _settingsService = settingsService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SensorManager service starting...");

        // Wait for configuration to be present
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
            _logger.LogError("SensorManager: Application is marked configured but no server info/webhook ID found.");
            return;
        }

        // 1. Register all sensors on startup
        await RegisterAllSensorsAsync(server);

        // 2. Periodic updates
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var intervalSeconds = await _settingsService.GetAsync<int>("SensorUpdateIntervalSeconds");
                var interval = intervalSeconds > 0 ? TimeSpan.FromSeconds(intervalSeconds) : DefaultUpdateInterval;

                await UpdateAllSensorsAsync(server);

                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic sensor update");
                await Task.Delay(10000, stoppingToken); // Wait a bit before retrying on error
            }
        }
    }

    private async Task RegisterAllSensorsAsync(HaServerInfo server)
    {
        _logger.LogInformation("Registering all sensors with Home Assistant...");
        
        foreach (var sensor in _sensors.Where(s => s.IsEnabled))
        {
            var registration = sensor.GetRegistration();
            bool success = await _haClient.RegisterSensorAsync(server, registration);
            if (success)
            {
                _logger.LogDebug("Successfully registered sensor: {UniqueId}", sensor.UniqueId);
            }
        }
    }

    private async Task UpdateAllSensorsAsync(HaServerInfo server)
    {
        var updates = _sensors
            .Where(s => s.IsEnabled)
            .Select(s => s.GetCurrentState())
            .ToList();

        if (updates.Count == 0) return;

        var results = await _haClient.UpdateSensorsAsync(server, updates);

        foreach (var (uniqueId, result) in results)
        {
            if (result.IsDisabled)
            {
                _logger.LogWarning("Sensor {UniqueId} is disabled in Home Assistant. Syncing state...", uniqueId);
                var sensor = _sensors.FirstOrDefault(s => s.UniqueId == uniqueId);
                if (sensor != null)
                {
                    sensor.IsEnabled = false;
                    // Persist this change if needed
                }
            }

            if (!result.Success && result.ErrorCode == "not_registered")
            {
                _logger.LogInformation("Sensor {UniqueId} not registered. Re-registering...", uniqueId);
                var sensor = _sensors.FirstOrDefault(s => s.UniqueId == uniqueId);
                if (sensor != null)
                {
                    await _haClient.RegisterSensorAsync(server, sensor.GetRegistration());
                }
            }
        }
    }
}
