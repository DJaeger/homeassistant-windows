# Home Assistant Windows Companion

[![GitHub Release](https://img.shields.io/github/v/release/FaserF/homeassistant-windows?style=flat-square)](https://github.com/FaserF/homeassistant-windows/releases)
[![Build Status](https://img.shields.io/github/actions/workflow/status/FaserF/homeassistant-windows/ci.yml?branch=main&style=flat-square)](https://github.com/FaserF/homeassistant-windows/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![Home Assistant](https://img.shields.io/badge/Home%20Assistant-Companion-blue.svg?style=flat-square&logo=home-assistant)](https://www.home-assistant.io/)

A native, high-performance Windows companion application for **Home Assistant**, built with **.NET 10** and **WinUI 3**. This app brings your Windows PC into your smart home ecosystem as a first-class citizen, matching the feature set and professional standards of the official mobile apps.

---

## 🏗 Architecture & Standards

This project is built from the ground up using **Clean Architecture** principles, ensuring long-term maintainability and modularity.

- **Core**: Domain models and abstractions, independent of platform APIs.
- **Infrastructure**: Platform-specific implementations (Windows APIs, Home Assistant REST/WS clients).
- **App**: Modern WinUI 3 interface following Fluent Design guidelines.
- **Standards**: 2026-compliant .NET 10 code, asynchronous patterns, and robust dependency injection.

---

## ✨ Key Features

### 📊 Real-Time Sensor Reporting
Provides granular data back to Home Assistant via the `mobile_app` integration:
- **CPU & Memory**: Instantaneous usage tracking.
- **Power Management**: Battery level, charging status, and AC power detection.
- **Productivity**: Foreground application/window title tracking (opt-in).
- **System Health**: Idle time (user presence) and network latency to HA.
- **Audio Environment**: Active output device identification.

### 🎮 Remote Windows Control
Control your Windows environment directly from Home Assistant dashboards and automations:
- **Media & Audio**: Master volume control, mute toggling, and media play/pause/skip.
- **Security**: Instant workstation lock.
- **Power Operations**: Shutdown, Restart, and Sleep commands.

### 🛠 Seamless User Experience
- **Interactive Setup**: A guided onboarding wizard that detects your HA instance via mDNS.
- **System Tray**: Lightweight background execution with a low memory footprint.
- **Autostart**: Built-in toggle to launch on Windows login.

---

## 🚀 Quick Start

1. **Download**: Grab the latest `HAWindowsCompanion.zip` from [Releases](https://github.com/fabian-be/homeassistant-windows/releases).
2. **Extract**: Unzip to a local folder (e.g., `%LOCALAPPDATA%\Programs`).
3. **Launch**: Open `HAWindowsCompanion.exe`.
4. **Connect**: Follow the "Setup Wizard" to authorize the app via OAuth2.
5. **Done**: Your PC will now appear as a new device in Home Assistant!

---

## 📖 Documentation

Detailed documentation is available in the `/docs` folder or via our [Online Documentation Site](https://fabian-be.github.io/homeassistant-windows/).

- [Installation Guide](docs/installation.md)
- [Feature Details](docs/features.md)
- [Troubleshooting](docs/troubleshooting.md)
- [Development Guide](docs/development.md)

---

## 🤝 Contributing

Contributions are welcome! Please see our [Development Guide](docs/development.md) for instructions on setting up the environment.

## 📜 License
This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for the full text.
Copyright © 2026 FaserF.
