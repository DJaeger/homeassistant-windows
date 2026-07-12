namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Manages application settings persisted locally.
/// </summary>
public interface ISettingsService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task<bool> ContainsKeyAsync(string key);
    Task RemoveAsync(string key);
    Task Reset();
}
