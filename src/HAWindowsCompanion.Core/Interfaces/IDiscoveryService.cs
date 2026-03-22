using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Discovers Home Assistant instances on the local network via mDNS.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>
    /// Scans for HA instances broadcasting _home-assistant._tcp.local.
    /// Returns when timeout elapses or when cancellation is requested.
    /// </summary>
    Task<IReadOnlyList<DiscoveredInstance>> DiscoverInstancesAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
