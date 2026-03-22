using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Handles OAuth2 authentication and token management for Home Assistant.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Initiates the OAuth2 authorization flow:
    /// opens browser to HA /auth/authorize, captures the callback code.
    /// </summary>
    Task<string> AuthorizeAsync(string instanceUrl);

    /// <summary>
    /// Exchanges authorization code for access + refresh tokens.
    /// POST {instanceUrl}/auth/token with grant_type=authorization_code.
    /// </summary>
    Task<TokenInfo> ExchangeCodeAsync(string instanceUrl, string code);

    /// <summary>
    /// Refreshes the access token using the refresh token.
    /// POST {instanceUrl}/auth/token with grant_type=refresh_token.
    /// </summary>
    Task<TokenInfo> RefreshTokenAsync(string instanceUrl, string refreshToken);

    /// <summary>
    /// Revokes the refresh token.
    /// POST {instanceUrl}/auth/token with action=revoke.
    /// </summary>
    Task RevokeTokenAsync(string instanceUrl, string refreshToken);

    /// <summary>
    /// Creates a TokenInfo from a long-lived access token.
    /// </summary>
    TokenInfo CreateFromLongLivedToken(string token);

    /// <summary>
    /// Gets a valid access token, refreshing if necessary.
    /// </summary>
    Task<string> GetValidAccessTokenAsync();
}
