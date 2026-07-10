using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Utilities;

public static class LocationHelpers
{
    public static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180);

    /**
     * Returns if the provided location is estimated to be in the zone.
     * This function will also consider accuracy, so if the GPS location is outside the zone but the
     * accuracy suggests that it could be in the zone, this function will still return `true`.
     */
    public static bool ZoneContainsLocationWithAccuracy(Zone zone, Location location) {

        double distance = CalculateDistanceMeters(
            location.Latitude,
            location.Longitude,
            zone.Latitude,
            zone.Longitude);

        return (
            distance - zone.RadiusMeters - location.Accuracy <= 0
        );
}
}
