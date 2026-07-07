namespace HAWindowsCompanion.Core.Models;

public sealed class Zone
{
    public required string Name { get; init; }
    public required string EntityId { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required double RadiusMeters { get; init; }
}
