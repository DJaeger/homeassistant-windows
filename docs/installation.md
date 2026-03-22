# Installation Guide

Setting up the Home Assistant Windows Companion is straightforward. Follow these steps to get your PC integrated into your smart home.

## 📥 Binary Download

The application is distributed as a self-contained, single-file executable packaged in a `.zip` archive. No installer is required, making it easy to run and portable.

1. Go to the [Releases](https://github.com/fabian-be/homeassistant-windows/releases) page.
2. Download the latest `HAWindowsCompanion.zip`.
3. Extract the contents to a permanent location on your PC (e.g., `C:\Program Files\HAWindowsCompanion` or `%LOCALAPPDATA%\Programs\HAWindowsCompanion`).
4. Double-click `HAWindowsCompanion.exe` to launch.

## 🛡️ Firewall & Connectivity

The application communicates with Home Assistant via:
- **mDNS (Multicast DNS)**: Used for automatic discovery on your local network.
- **HTTP/HTTPS**: Standard API communication.
- **WebSockets**: Real-time command handling.

Ensure that:
- Your PC and Home Assistant are on the same local network (for discovery).
- Your Windows Firewall allows outgoing connections to your Home Assistant URL (default port `8123`).
- If using Home Assistant Cloud (Nabu Casa), the app will automatically resolve and use your remote URL when off-site.

## 🚀 First-Run Setup

Upon first launch, the **Setup Wizard** will appear:
1. **Discovery**: The app will scan for Home Assistant instances. If found, select your instance. Otherwise, enter your URL manually (e.g., `http://homeassistant.local:8123`).
2. **Authentication**: A browser window will open for you to log in to Home Assistant and authorize the "Windows Companion".
3. **Registration**: The app will register itself as a new device in Home Assistant.

## ⚙️ Start on Boot

To ensure sensors are always up-to-date:
1. Open the app and go to **Settings**.
2. Toggle **Launch at Startup** to ON.
3. This creates a secure registry entry in `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
