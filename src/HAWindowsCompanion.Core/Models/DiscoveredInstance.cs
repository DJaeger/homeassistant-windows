namespace HAWindowsCompanion.Core.Models;

/// <summary>
/// Represents a discovered Home Assistant instance via mDNS.
/// </summary>
public sealed class DiscoveredInstance
{
    public required string HostName { get; set; }
    public required string IpAddress { get; set; }
    public int Port { get; set; } = 8123;
    public string? BaseUrl { get; set; }
    public string? Version { get; set; }
    public string? Uuid { get; set; }

    public string Url => BaseUrl ?? $"http://{IpAddress}:{Port}";

    public override string ToString() => $"{HostName} ({Url})";
}
