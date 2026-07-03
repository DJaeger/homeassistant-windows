using Windows.Security.Credentials;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Platform;

/// <summary>
/// Implements secure storage for tokens and server info using Windows PasswordVault.
/// </summary>
public sealed class WindowsCredentialStore : ICredentialStore
{
    private const string ResourceName = "HAWindowsCompanion";
    private const string TokenKey = "UserTokens";
    private const string ServerInfoKey = "ServerInfo";
    private readonly PasswordVault _vault = new();

    public Task SaveTokenAsync(TokenInfo token)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(token);
        _vault.Add(new PasswordCredential(ResourceName, TokenKey, json));
        return Task.CompletedTask;
    }

    public Task<TokenInfo?> LoadTokenAsync()
    {
        try
        {
            var credential = _vault.Retrieve(ResourceName, TokenKey);
            credential.RetrievePassword();
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<TokenInfo>(credential.Password));
        }
        catch
        {
            return Task.FromResult<TokenInfo?>(null);
        }
    }

    public Task DeleteTokenAsync()
    {
        try
        {
            var credential = _vault.Retrieve(ResourceName, TokenKey);
            _vault.Remove(credential);
        }
        catch { }
        return Task.CompletedTask;
    }

    public Task SaveServerInfoAsync(HaServerInfo serverInfo)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(serverInfo);
        _vault.Add(new PasswordCredential(ResourceName, ServerInfoKey, json));
        return Task.CompletedTask;
    }

    public Task<HaServerInfo?> LoadServerInfoAsync()
    {
        try
        {
            var credential = _vault.Retrieve(ResourceName, ServerInfoKey);
            credential.RetrievePassword();
            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<HaServerInfo>(credential.Password));
        }
        catch
        {
            return Task.FromResult<HaServerInfo?>(null);
        }
    }

    public Task DeleteServerInfoAsync()
    {
        try
        {
            var credential = _vault.Retrieve(ResourceName, ServerInfoKey);
            _vault.Remove(credential);
        }
        catch { }
        return Task.CompletedTask;
    }
}
