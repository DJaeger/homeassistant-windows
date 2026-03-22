namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Manages auto-start behavior (e.g., Windows startup registry entry).
/// </summary>
public interface IStartupManager
{
    bool IsStartupEnabled { get; }
    void EnableStartup();
    void DisableStartup();
}
