using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Interfaces;

public interface IHomeZoneService
{
    Task<HomeZoneInfo?> GetHomeZoneAsync();
}
