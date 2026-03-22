using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Sensors;

public class LocationSensor : ISensorProvider
{
    private readonly Geolocator _geolocator;

    public string UniqueId => "location";
    public string Name => "Location";
    public bool IsEnabled { get; set; } = true;

    public LocationSensor()
    {
        _geolocator = new Geolocator { DesiredAccuracyInMeters = 50 };
    }

    public SensorRegistration GetRegistration()
    {
        return new SensorRegistration
        {
            UniqueId = UniqueId,
            Name = Name,
            Type = "sensor",
            Icon = "mdi:map-marker",
            DeviceClass = "location"
        };
    }

    public SensorUpdate GetCurrentState()
    {
        try
        {
            // We use Task.Run because GetGeopositionAsync can be slow/blocking
            var posTask = _geolocator.GetGeopositionAsync().AsTask();
            posTask.Wait(2000); // 2 second timeout

            if (posTask.IsCompletedSuccessfully)
            {
                var pos = posTask.Result.Coordinate.Point.Position;
                return new SensorUpdate
                {
                    UniqueId = UniqueId,
                    State = $"{pos.Latitude}, {pos.Longitude}",
                    Attributes = new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "latitude", pos.Latitude },
                        { "longitude", pos.Longitude },
                        { "gps_accuracy", posTask.Result.Coordinate.Accuracy },
                        { "altitude", pos.Altitude }
                    }
                };
            }
        }
        catch { }

        return new SensorUpdate
        {
            UniqueId = UniqueId,
            State = "Unknown",
            Attributes = new System.Collections.Generic.Dictionary<string, object>()
        };
    }
}
