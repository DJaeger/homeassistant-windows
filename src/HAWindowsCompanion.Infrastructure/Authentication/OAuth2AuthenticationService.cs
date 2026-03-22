using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Authentication;

/// <summary>
/// Implements Home Assistant OAuth2 authentication flow.
/// Opens a local loopback HTTP listener to capture the auth callback.
/// </summary>
public sealed class OAuth2AuthenticationService : IAuthenticationService
{
    private const string ClientId = "https://github.com/home-assistant/homeassistant-windows";
    private const string RedirectPath = "/auth/callback";
    private const int CallbackPort = 18123;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<OAuth2AuthenticationService> _logger;

    private TokenInfo? _currentToken;
    private string? _instanceUrl;

    public OAuth2AuthenticationService(
        IHttpClientFactory httpClientFactory,
        ICredentialStore credentialStore,
        ILogger<OAuth2AuthenticationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public async Task<string> AuthorizeAsync(string instanceUrl)
    {
        _instanceUrl = instanceUrl.TrimEnd('/');

        var redirectUri = $"http://localhost:{CallbackPort}{RedirectPath}";
        var authorizeUrl = $"{_instanceUrl}/auth/authorize" +
            $"?client_id={Uri.EscapeDataString(ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&state={Uri.EscapeDataString(_instanceUrl)}";

        _logger.LogInformation("Starting OAuth2 flow for {InstanceUrl}", _instanceUrl);

        // Start local listener for callback
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{CallbackPort}/");
        listener.Start();

        // Open system browser for authentication
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = authorizeUrl,
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);

        // Wait for callback
        var context = await listener.GetContextAsync();
        var code = context.Request.QueryString["code"]
            ?? throw new InvalidOperationException("No authorization code received");

        // Send success response to browser
        var responseHtml = """
            <html><body style="font-family:Segoe UI,sans-serif;text-align:center;padding:40px">
            <h2>✅ Authentication Successful</h2>
            <p>You can close this tab and return to the app.</p>
            </body></html>
            """;
        var buffer = Encoding.UTF8.GetBytes(responseHtml);
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.Close();

        _logger.LogInformation("OAuth2 authorization code received");
        return code;
    }

    public async Task<TokenInfo> ExchangeCodeAsync(string instanceUrl, string code)
    {
        _instanceUrl = instanceUrl.TrimEnd('/');
        var redirectUri = $"http://localhost:{CallbackPort}{RedirectPath}";

        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = ClientId
        });

        var response = await client.PostAsync($"{_instanceUrl}/auth/token", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<TokenInfo>(json)
            ?? throw new InvalidOperationException("Failed to parse token response");

        token.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
        _currentToken = token;

        await _credentialStore.SaveTokenAsync(token);
        _logger.LogInformation("OAuth2 tokens acquired, expires in {ExpiresIn}s", token.ExpiresIn);

        return token;
    }

    public async Task<TokenInfo> RefreshTokenAsync(string instanceUrl, string refreshToken)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = ClientId
        });

        var response = await client.PostAsync($"{instanceUrl.TrimEnd('/')}/auth/token", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<TokenInfo>(json)
            ?? throw new InvalidOperationException("Failed to parse refresh response");

        // Refresh response does not include refresh_token, keep the existing one
        token.RefreshToken = refreshToken;
        token.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
        _currentToken = token;

        await _credentialStore.SaveTokenAsync(token);
        _logger.LogDebug("Access token refreshed, expires in {ExpiresIn}s", token.ExpiresIn);

        return token;
    }

    public async Task RevokeTokenAsync(string instanceUrl, string refreshToken)
    {
        var client = _httpClientFactory.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["token"] = refreshToken,
            ["action"] = "revoke"
        });

        await client.PostAsync($"{instanceUrl.TrimEnd('/')}/auth/token", content);
        await _credentialStore.DeleteTokenAsync();
        _currentToken = null;

        _logger.LogInformation("Refresh token revoked");
    }

    public TokenInfo CreateFromLongLivedToken(string token)
    {
        _currentToken = new TokenInfo
        {
            AccessToken = token,
            RefreshToken = null,
            ExpiresIn = 0,
            ExpiresAt = DateTimeOffset.MaxValue
        };

        return _currentToken;
    }

    public async Task<string> GetValidAccessTokenAsync()
    {
        if (_currentToken is null)
        {
            _currentToken = await _credentialStore.LoadTokenAsync();
            if (_currentToken is null)
                throw new InvalidOperationException("No authentication token available. Please authenticate first.");

            var serverInfo = await _credentialStore.LoadServerInfoAsync();
            _instanceUrl = serverInfo?.InstanceUrl;
        }

        // Long-lived tokens never expire
        if (_currentToken.IsLongLived)
            return _currentToken.AccessToken;

        // Refresh if less than 60 seconds until expiry
        if (_currentToken.ExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(60))
        {
            if (string.IsNullOrEmpty(_currentToken.RefreshToken) || string.IsNullOrEmpty(_instanceUrl))
                throw new InvalidOperationException("Cannot refresh token: missing refresh token or instance URL");

            _currentToken = await RefreshTokenAsync(_instanceUrl, _currentToken.RefreshToken);
        }

        return _currentToken.AccessToken;
    }
}
