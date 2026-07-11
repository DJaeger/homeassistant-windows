using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.Infrastructure.Commands;

/// <summary>
/// Connects to Home Assistant via WebSockets to receive and dispatch commands.
/// Also handles the initial authentication handshake for the WebSocket connection.
/// </summary>
public sealed class CommandDispatcher(
        IEnumerable<ICommandHandler> _handlers,
        IAuthenticationService _authService,
        ICredentialStore _credentialStore,
        ISettingsService _settingsService,
        INotificationService _notificationService,
        ILogger<CommandDispatcher> _logger
): BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CommandDispatcher service starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isConfigured = await _settingsService.GetAsync<bool>("IsConfigured");
                if (!isConfigured)
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                var server = await _credentialStore.LoadServerInfoAsync();
                if (server == null)
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                await RunWebSocketLoopAsync(server, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WebSocket command loop. Retrying in 10s...");
                await Task.Delay(10000, stoppingToken);
            }
        }
    }

    private async Task RunWebSocketLoopAsync(HaServerInfo server, CancellationToken stoppingToken)
    {
        using var ws = new ClientWebSocket();
        var wsUri = new Uri(server.InstanceUrl.Replace("http", "ws") + "/api/websocket");

        _logger.LogInformation("Connecting to Home Assistant WebSocket at {Uri}", wsUri);
        await ws.ConnectAsync(wsUri, stoppingToken);

        // 1. Receive 'auth_required'
        var authRequired = await ReceiveMessageAsync(ws, stoppingToken);
        if (authRequired?.GetProperty("type").GetString() != "auth_required")
        {
             throw new InvalidOperationException("Expected 'auth_required' message from Home Assistant");
        }

        // 2. Send 'auth'
        var accessToken = await _authService.GetValidAccessTokenAsync();
        var authPayload = new { type = "auth", access_token = accessToken };
        await SendMessageAsync(ws, authPayload, stoppingToken);

        // 3. Receive 'auth_ok'
        var authOk = await ReceiveMessageAsync(ws, stoppingToken);
        if (authOk?.GetProperty("type").GetString() != "auth_ok")
        {
            throw new InvalidOperationException("WebSocket authentication failed");
        }

        _logger.LogInformation("WebSocket authentication successful");

        // 4. Listen for commands/events
        // Note: The mobile_app integration usually triggers actions via webhooks or services that would fire events.
        // For the companion app, we listen for events that match our command identifiers.
        
        // Example: Subscribe to custom events if needed, but typically mobile_app registration 
        // allows HA to send notifications/commands via the cloudhook/webhook.
        // However, for real-time bi-directional local control, we can subscribe to events here.
        
        var subscribeEventsPayload = new
        { 
            id = 1, 
            type = "subscribe_events", 
            event_type = "mobile_app_command" 
        };
        await SendMessageAsync(ws, subscribeEventsPayload, stoppingToken);

        var subscribePushPayload = new
        {
            id = 2,
            type = "mobile_app/push_notification_channel",
            webhook_id = server.WebhookId,
            support_confirm = false
        };
        await SendMessageAsync(ws, subscribePushPayload, stoppingToken);

        while (ws.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
        {
            var message = await ReceiveMessageAsync(ws, stoppingToken);
            if (message == null) continue;

            if (message.Value.TryGetProperty("type", out var type) && type.GetString() == "event")
            {
                var eventData = message.Value.GetProperty("event");
                
                // Handle commands
                if (eventData.TryGetProperty("data", out var data))
                {
                    await HandleCommandAsync(data);
                }
                
                // Handle notifications (Official Companion App Standard)
                if (eventData.TryGetProperty("message", out var msgProp))
                {
                    var title = eventData.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : "Home Assistant";
                    var messageText = msgProp.GetString();
                    var imageUrl = eventData.TryGetProperty("data", out var dataProp) && dataProp.TryGetProperty("image", out var imgProp) ? imgProp.GetString() : null;
                    
                    _notificationService.ShowNotification(title!, messageText!, imageUrl);
                }
            }
        }
    }

    private async Task HandleCommandAsync(JsonElement data)
    {
        if (!data.TryGetProperty("command", out var commandProp)) return;
        
        string command = commandProp.GetString() ?? "";
        _logger.LogInformation("Received command from Home Assistant: {Command}", command);

        var handler = _handlers.FirstOrDefault(h => h.CommandType.Equals(command, StringComparison.OrdinalIgnoreCase));
        if (handler != null)
        {
            try
            {
                await handler.ExecuteAsync(data);
                _logger.LogInformation("Command {Command} executed successfully", command);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command {Command}", command);
            }
        }
        else
        {
            _logger.LogWarning("No handler registered for command: {Command}", command);
        }
    }

    private async Task SendMessageAsync(ClientWebSocket ws, object payload, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task<JsonElement?> ReceiveMessageAsync(ClientWebSocket ws, CancellationToken ct)
    {
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
        if (result.MessageType == WebSocketMessageType.Close) return null;

        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}
