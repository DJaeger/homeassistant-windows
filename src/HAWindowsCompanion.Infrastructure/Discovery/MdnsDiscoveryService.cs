using Microsoft.Extensions.Logging;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;
using Zeroconf;

namespace HAWindowsCompanion.Infrastructure.Discovery;

/// <summary>
/// Discovers Home Assistant instances on the local network via mDNS/Zeroconf.
/// Searches for the _home-assistant._tcp.local. service type.
/// </summary>
public sealed class MdnsDiscoveryService : IDiscoveryService
{
    private const string ServiceType = "_home-assistant._tcp.local.";
    private readonly ILogger<MdnsDiscoveryService> _logger;

    public MdnsDiscoveryService(ILogger<MdnsDiscoveryService> logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<DiscoveredInstance>> DiscoverInstancesAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting mDNS discovery for {ServiceType}", ServiceType);

        var results = new List<DiscoveredInstance>();

        try
        {
            var responses = await ZeroconfResolver.ResolveAsync(
                ServiceType,
                scanTime: timeout,
                cancellationToken: cancellationToken);

            foreach (var host in responses)
            {
                var ip = host.IPAddress;
                var port = 8123;
                string? version = null;
                string? baseUrl = null;
                string? uuid = null;

                foreach (var service in host.Services.Values)
                {
                    port = service.Port;
                    if (service.Properties != null)
                    {
                        foreach (var prop in service.Properties)
                        {
                            if (prop.TryGetValue("version", out var v))
                                version = v;
                            if (prop.TryGetValue("base_url", out var b))
                                baseUrl = b;
                            if (prop.TryGetValue("uuid", out var u))
                                uuid = u;
                        }
                    }
                }

                var instance = new DiscoveredInstance
                {
                    HostName = host.DisplayName,
                    IpAddress = ip,
                    Port = port,
                    BaseUrl = baseUrl,
                    Version = version,
                    Uuid = uuid
                };

                results.Add(instance);
                _logger.LogInformation("Discovered HA instance: {Instance}", instance);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("mDNS discovery was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "mDNS discovery failed");
        }

        _logger.LogInformation("mDNS discovery completed, found {Count} instance(s)", results.Count);
        return results;
    }
}
