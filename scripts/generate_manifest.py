import datetime
import json
import os
import re

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUTPUT_FILE = os.path.join(PROJECT_ROOT, "project_manifest.json")
CONNECTIONS_FILE = os.path.join(PROJECT_ROOT, "project_connections.json")

IGNORE_DIRS = {
    ".git", "__pycache__", "node_modules", "dist", "build", "coverage",
    ".pytest_cache", ".idea", ".vscode", "bin", "obj", "publish_local",
    "publish", ".system_generated", "scratch", "docs", ".github"
}
IGNORE_FILES = {
    "package-lock.json", "yarn.lock", ".DS_Store", "project_manifest.json",
    "project_connections.json", "HAWindowsCompanion.code-workspace"
}
ALLOWED_EXTENSIONS = {
    ".cs", ".xaml", ".sh", ".yml", ".yaml", ".md", ".json", ".toml", ".py"
}

def generate_file_tree(startpath):
    tree = {}
    for root, dirs, files in os.walk(startpath):
        dirs[:] = [d for d in dirs if d not in IGNORE_DIRS and not d.startswith(".")]
        rel_path = os.path.relpath(root, startpath)
        rel_path_key = rel_path.replace(os.sep, "/")

        valid_files = []
        for f in files:
            if f in IGNORE_FILES or f.startswith("."):
                continue
            ext = os.path.splitext(f)[1].lower()
            if ext in ALLOWED_EXTENSIONS or f in {"LICENSE", "VERSION"}:
                valid_files.append(f)

        if valid_files:
            tree[rel_path_key] = sorted(valid_files)
    return tree

def parse_cs_file(filepath):
    """
    Scans a C# file to find class/interface declarations, namespaces,
    inherited types, properties, and public methods.
    """
    classes = []
    interfaces = []
    
    with open(filepath, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()

    # Find namespace
    namespace_match = re.search(r"namespace\s+([\w\.]+);?", content)
    namespace = namespace_match.group(1) if namespace_match else "Unknown"

    # Find classes & interfaces
    # Matching class/interface Name : Inherited1, Inherited2
    class_matches = re.finditer(
        r"(public|internal|private)?\s*(sealed|partial|abstract)?\s*class\s+(\w+)(?:\s*:\s*([\w\.\s,<>]+))?",
        content
    )
    for m in class_matches:
        cls_name = m.group(3)
        inherits = [i.strip() for i in m.group(4).split(",")] if m.group(4) else []
        
        # Find methods (public and internal)
        methods = re.findall(
            r"(public|internal)\s+(async\s+)?(?:Task<[\w\.\s,<>]+>|Task|[\w\.\s,<>]+)\s+(\w+)\s*\(([^)]*)\)",
            content
        )
        method_names = []
        for meth in methods:
            if meth[2] not in {"OnLaunched", "InitializeComponent", "Dispose", "OnClosing"}:
                method_names.append(meth[2])
                
        classes.append({
            "name": cls_name,
            "namespace": namespace,
            "inherits": inherits,
            "methods": method_names
        })

    interface_matches = re.finditer(
        r"(public|internal)?\s*interface\s+(\w+)(?:\s*:\s*([\w\.\s,<>]+))?",
        content
    )
    for m in interface_matches:
        int_name = m.group(2)
        methods = re.findall(r"(?:Task<[\w\.\s,<>]+>|Task|[\w\.\s,<>]+)\s+(\w+)\s*\(([^)]*)\);", content)
        method_names = [meth[0] for meth in methods]
        interfaces.append({
            "name": int_name,
            "namespace": namespace,
            "methods": method_names
        })

    return classes, interfaces

def generate_manifest():
    print("Generating homeassistant-windows Project Manifest...")
    
    classes_list = []
    interfaces_list = []
    
    for root, dirs, files in os.walk(PROJECT_ROOT):
        dirs[:] = [d for d in dirs if d not in IGNORE_DIRS and not d.startswith(".")]
        for f in files:
            if f.endswith(".cs"):
                path = os.path.join(root, f)
                c, i = parse_cs_file(path)
                classes_list.extend(c)
                interfaces_list.extend(i)

    # Categorize classes
    viewmodels = [c for c in classes_list if c["name"].endswith("ViewModel")]
    views = [c for c in classes_list if c["name"].endswith("Page") or c["name"] == "MainWindow"]
    sensors = [c for c in classes_list if c["name"].endswith("Sensor") or "ISensorProvider" in c["inherits"]]
    command_handlers = [c for c in classes_list if c["name"].endswith("CommandHandler") or "ICommandHandler" in c["inherits"]]
    services = [
        c for c in classes_list 
        if c not in viewmodels and c not in views and c not in sensors and c not in command_handlers
        and any(keyword in c["name"] for keyword in ["Service", "Store", "Manager", "Dispatcher", "Tracker"])
    ]

    manifest = {
        "project": "HAWindowsCompanion",
        "purpose": "Home Assistant Windows Companion application running in System Tray, pushing sensors and receiving commands.",
        "timestamp": datetime.datetime.now().isoformat(),
        "stack": {
            "platform": "WinUI 3 (Windows App SDK)",
            "runtime": ".NET 10.0-windows",
            "mvvm_toolkit": "CommunityToolkit.Mvvm"
        },
        "architecture": {
            "viewmodels": viewmodels,
            "views": views,
            "sensors": sensors,
            "command_handlers": command_handlers,
            "services": services,
            "interfaces": interfaces_list
        },
        "file_tree": generate_file_tree(PROJECT_ROOT)
    }

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)
    print(f"Manifest generated: {os.path.getsize(OUTPUT_FILE)} bytes.")

    # Generate project connections map
    generate_connections()

def generate_connections():
    print("Generating Dynamic Connection Map...")
    
    # We will map key feature flows to files
    connections = {
        "timestamp": datetime.datetime.now().isoformat(),
        "features": {
            "sensors": {
                "description": "System state sensors (CPU, RAM, Idle Time, Audio, Active Window, SSID) published to Home Assistant.",
                "files": [
                    "src/HAWindowsCompanion.Infrastructure/Sensors/SensorManager.cs",
                    "src/HAWindowsCompanion.Infrastructure/Sensors/CpuUsageSensor.cs",
                    "src/HAWindowsCompanion.Infrastructure/Sensors/MemoryUsageSensor.cs",
                    "src/HAWindowsCompanion.Infrastructure/Sensors/BatteryStatusSensor.cs",
                    "src/HAWindowsCompanion.Infrastructure/Sensors/ActiveWindowSensor.cs",
                    "src/HAWindowsCompanion.Infrastructure/Sensors/SystemIdleTimeSensor.cs",
                    "src/HAWindowsCompanion.Infrastructure/Sensors/AudioOutputDeviceSensor.cs",
                    "src/HAWindowsCompanion.Infrastructure/Sensors/NetworkSsidSensor.cs",
                    "src/HAWindowsCompanion.Core/Interfaces/ISensorProvider.cs"
                ]
            },
            "commands": {
                "description": "Bi-directional WebSocket control commands from Home Assistant handled locally.",
                "files": [
                    "src/HAWindowsCompanion.Infrastructure/Commands/CommandDispatcher.cs",
                    "src/HAWindowsCompanion.Infrastructure/Commands/VolumeCommandHandler.cs",
                    "src/HAWindowsCompanion.Infrastructure/Commands/LockSessionCommandHandler.cs",
                    "src/HAWindowsCompanion.Infrastructure/Commands/ShutdownCommandHandler.cs",
                    "src/HAWindowsCompanion.Infrastructure/Commands/MediaPlayPauseCommandHandler.cs",
                    "src/HAWindowsCompanion.Core/Interfaces/ICommandHandler.cs"
                ]
            },
            "authentication_flow": {
                "description": "OAuth2 auth callback with loopback HttpListener to obtain access tokens.",
                "files": [
                    "src/HAWindowsCompanion.Infrastructure/Authentication/OAuth2AuthenticationService.cs",
                    "src/HAWindowsCompanion.Infrastructure/Platform/WindowsCredentialStore.cs",
                    "src/HAWindowsCompanion.Core/Interfaces/IAuthenticationService.cs"
                ]
            },
            "navigation_and_ui": {
                "description": "MVVM navigation service and WinUI pages (Main, Settings, SetupWizard).",
                "files": [
                    "src/HAWindowsCompanion.App/Services/NavigationService.cs",
                    "src/HAWindowsCompanion.App/MainWindow.xaml.cs",
                    "src/HAWindowsCompanion.App/Views/MainPage.xaml",
                    "src/HAWindowsCompanion.App/Views/SettingsPage.xaml",
                    "src/HAWindowsCompanion.App/Views/SetupWizardPage.xaml"
                ]
            },
            "logging_system": {
                "description": "Daily rolling file logger that serializes writes across threads using a shared lock.",
                "files": [
                    "src/HAWindowsCompanion.Infrastructure/Logging/FileLogger.cs",
                    "src/HAWindowsCompanion.Infrastructure/Logging/FileLoggerProvider.cs",
                    "src/HAWindowsCompanion.Infrastructure/Logging/FileLoggerOptions.cs"
                ]
            }
        }
    }

    with open(CONNECTIONS_FILE, "w", encoding="utf-8") as f:
        json.dump(connections, f, indent=2)
    print(f"Connection Map generated: {os.path.getsize(CONNECTIONS_FILE)} bytes.")

if __name__ == "__main__":
    generate_manifest()
