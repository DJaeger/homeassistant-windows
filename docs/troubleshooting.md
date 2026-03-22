# Troubleshooting

Encountering issues? Follow this guide to resolve common problems with the Windows Companion.

## 🌐 Connectivity Issues

### Instance Not Discovered
- **mDNS/Zeroconf**: Ensure the `zeroconf` integration is enabled in your `configuration.yaml` (it is enabled by default in `default_config:`).
- **Subnets**: mDNS often doesn't cross between different VLANs or subnets without an mDNS reflector/repeater.
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

For advanced troubleshooting, logs are stored in `%LOCALAPPDATA%\HAWindowsCompanion\logs`. Please include these when reporting issues on GitHub.
