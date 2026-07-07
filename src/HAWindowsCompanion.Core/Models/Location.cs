namespace HAWindowsCompanion.Core.Models
{
    public sealed class Location
    {
        public required double Latitude { get; init; }
        public required double Longitude { get; init; }
        public int Accuracy { get; init; }
    }
}
