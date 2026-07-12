# Troubleshooting

Encountering issues? Follow this guide to resolve common problems with the Windows Companion.

## 🌐 Connectivity Issues

### Instance Not Discovered
- **mDNS/Zeroconf**: Ensure the `zeroconf` integration is enabled in your `configuration.yaml` (it is enabled by default in `default_config:`).
- **Subnets**: mDNS often doesn't cross between different VLANs or subnets without an mDNS reflector/repeater.
- **Multiple Interfaces**: By default, Home Assistant only supports mDNS on the primary interface and, consequently, only on the primary network
- **Manual Entry**: If discovery fails, you can always enter your URL manually in the format `http://192.168.1.x:8123`.

### "Connection Refused" or Timeouts
- **IP Address**: If you changed your HA IP address, you may need to update the URL in the app's Settings.
- **VPN**: If you are on a VPN, ensure it allows local network traffic, or use your external/Cloudhook URL.

## 📊 Sensor Issues

### Sensors Not Updating
- **Update Interval**: The default update interval is 60 seconds. You can decrease this in Settings, but note that very low intervals (e.g., < 5s) may increase CPU usage.
- **Disabled Entities**: Check in Home Assistant under **Settings > Devices & Services > Entities**. Searching for your PC name. Ensure the entities are enabled.

### Incorrect Values
- **CPU/RAM**: The values are pulled from Windows Performance Counters. If these are corrupted on your system, the app may report `0`.

## 🎮 Command Issues

### "Command Not Found" or No Response
- **WebSocket Connection**: Commands rely on a persistent WebSocket. If your PC has an unstable internet connection, commands might be delayed.
- **App Not Running**: Ensure the "Home Assistant" icon is visible in your system tray. If it's not there, the service is not running.

## 📝 Logging

### Enabling File Logging

File logging is **disabled by default** to save storage space. You can enable it in the Settings when needed:

1. Open **Settings** via the system tray icon
2. Scroll to the **"Debugging"** section
3. Enable **"Enable File Logging"**
4. Click **"Restart App"** to apply the change

**⚠️ Note:** An application restart is required for file logging to become active.

### Finding Log Files

After activation, all logs are automatically saved to:

```
%LOCALAPPDATA%\HAWindowsCompanion\logs\
```

You can open the folder directly via the **"Open Log-Folder"** button in Settings.

### Log Format

Each log line follows this format:

```
[YYYY-MM-DD HH:mm:ss] [LogLevel] Category: Message
```

**Example:**
```
[2026-03-15 14:23:45] [Information] HAWindowsCompanion.Infrastructure.Commands.CommandDispatcher: CommandDispatcher service starting...
[2026-03-15 14:23:46] [Warning] HAWindowsCompanion.Infrastructure.Sensors.SensorManager: Failed to update sensor battery_level
[2026-03-15 14:23:47] [Error] HAWindowsCompanion.Infrastructure.Api.HomeAssistantApiClient: Connection refused
```

**Log Levels:**
- `[Debug]` - Detailed information for developers
- `[Information]` - General status messages
- `[Warning]` - Warnings that require attention
- `[Error]` - Errors that should be fixed
- `[Critical]` - Critical errors affecting the application

### Log Rotation and Storage

- **Daily new file:** The app automatically creates a new log file per day (format: `app-YYYY-MM-DD.log`)
- **Automatic cleanup:** Log files older than **7 days** are automatically removed
- **Storage usage:** Under normal operation, logs only consume a few MB

### Providing Logs for GitHub Issues

When reporting a problem on GitHub, please include the relevant log files:

1. Enable file logging (if not already enabled)
2. Reproduce the issue
3. Open the log folder via **"Open Log-Folder"**
4. Upload the most recent `app-YYYY-MM-DD.log` file

**Note:** Please check the logs for sensitive information (e.g., passwords, API keys) before sharing them publicly.
