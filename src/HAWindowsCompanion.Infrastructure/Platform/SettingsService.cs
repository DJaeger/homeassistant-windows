using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using HAWindowsCompanion.Core.Interfaces;

namespace HAWindowsCompanion.Infrastructure.Platform;

/// <summary>
/// Manages application settings stored in a JSON file in LocalAppData.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private Dictionary<string, JsonElement> _settings = new();
    private readonly object _lock = new();

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDir = Path.Combine(appData, "HAWindowsCompanion");
        Directory.CreateDirectory(settingsDir);
        _settingsFilePath = Path.Combine(settingsDir, "settings.json");
        Load();
    }

    public Task<T?> GetAsync<T>(string key)
    {
        lock (_lock)
        {
            if (_settings.TryGetValue(key, out var element))
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(element.GetRawText()));
            }
        }
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(value);
            _settings[key] = JsonDocument.Parse(json).RootElement.Clone();
            Save();
        }
        return Task.CompletedTask;
    }

    public Task<bool> ContainsKeyAsync(string key)
    {
        lock (_lock) return Task.FromResult(_settings.ContainsKey(key));
    }

    public Task RemoveAsync(string key)
    {
        lock (_lock)
        {
            if (_settings.Remove(key)) Save();
        }
        return Task.CompletedTask;
    }

    private void Load()
    {
        if (!File.Exists(_settingsFilePath)) return;
        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            _settings = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
        }
        catch { }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { }
    }
}
