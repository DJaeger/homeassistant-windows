using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Securely stores and retrieves credentials using the OS credential store.
/// </summary>
public interface ICredentialStore
{
    Task SaveTokenAsync(TokenInfo token);
    Task<TokenInfo?> LoadTokenAsync();
    Task DeleteTokenAsync();

    Task SaveServerInfoAsync(HaServerInfo serverInfo);
    Task<HaServerInfo?> LoadServerInfoAsync();
    Task DeleteServerInfoAsync();
}
