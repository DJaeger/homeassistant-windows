# Features Overview

The Home Assistant Windows Companion transforms your PC into a smart citizen of your home automation ecosystem.

## 📊 Sensors (Incoming Data)

The application reports real-time data back to Home Assistant using the standard `mobile_app` integration.

### 💻 System Performance
- **CPU Usage**: Reports the percentage of processor utility across all cores.
- **RAM Usage**: Reports the percentage of committed memory in use.

### 🔋 Power & Battery
- **Battery Level**: Current charge percentage.
- **Power State**: Detects if the device is on AC power or battery.
- **Charging Status**: Indicates if the battery is currently being charged.

### 🖥️ Activity & Presence
- **Active Window**: Reports the title of the window currently in focus. *Note: For privacy, this can be disabled in settings.*
- **System Idle Time**: Tracks the number of seconds since the last user interaction (Keyboard/Mouse), enabling precise "User Presence" automations.

### 🔊 Audio & Network
- **Audio Output**: Identifies the currently active playback device (e.g., "Realtek High Definition Audio").
- **Network Latency**: Reports the ping round-trip time to your Home Assistant instance, useful for monitoring network stability.

---

## 🎮 Commands (Outgoing Control)

Trigger actions on your Windows PC directly from Home Assistant via services or dashboard buttons.

### 🔊 Audio Control
- **Set Volume**: Adjust master volume from 0% to 100%.
- **Mute Toggle**: Instantly silence or restore audio.
- **Step Up/Down**: Incremental volume adjustments.

### 📺 Media Navigation
- **Play / Pause**: Control active media players (Spotify, YouTube in browser, VLC, etc.).
- **Skip / Previous**: Navigate through playlists.

### 🔑 Security & Session
- **Lock Session**: Instantly locks the Windows workstation.
- **System Power**:
  - **Shutdown**: Gracefully shuts down the PC.
  - **Restart**: Reboots the machine.
  - **Sleep**: Puts the PC into low-power sleep mode.
