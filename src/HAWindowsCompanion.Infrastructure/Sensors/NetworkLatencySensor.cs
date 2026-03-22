using System.Net.NetworkInformation;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;
using Microsoft.Extensions.Logging;

namespace HAWindowsCompanion.Infrastructure.Sensors;

/// <summary>
/// Reports network latency (ping round-trip time) to the Home Assistant instance.
/// </summary>
public sealed class NetworkLatencySensor : ISensorProvider
{
    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<NetworkLatencySensor> _logger;

    public string UniqueId => "network_latency";
    public string Name => "Network Latency";
    public bool IsEnabled { get; set; } = true;

    public NetworkLatencySensor(ICredentialStore credentialStore, ILogger<NetworkLatencySensor> logger)
    {
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public SensorRegistration GetRegistration() => new()
    {
        UniqueId = UniqueId,
        Name = Name,
        Type = "sensor",
        Icon = "mdi:speedometer",
        UnitOfMeasurement = "ms",
        StateClass = "measurement",
        EntityCategory = "diagnostic",
        State = -1
    };

    public SensorUpdate GetCurrentState() => new()
    {
        UniqueId = UniqueId,
        Type = "sensor",
        Icon = "mdi:speedometer",
        State = GetLatencyMs()
    };

    private long GetLatencyMs()
    {
        try
        {
            var serverInfo = _credentialStore.LoadServerInfoAsync().GetAwaiter().GetResult();
            if (serverInfo is null) return -1;

            var uri = new Uri(serverInfo.InstanceUrl);
            using var ping = new Ping();
            var reply = ping.Send(uri.Host, timeout: 5000);

            return reply.Status == IPStatus.Success ? reply.RoundtripTime : -1;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ping to HA instance failed");
            return -1;
        }
    }
}
