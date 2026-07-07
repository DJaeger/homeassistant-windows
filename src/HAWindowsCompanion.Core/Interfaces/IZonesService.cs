using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Interfaces;

public interface IZonesService
{
    Task<List<Zone>?> GetZonesAsync();

    Task<List<Zone>> GetZonesForLocationAsync(Location location);

}
