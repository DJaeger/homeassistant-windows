using Microsoft.Win32;
using System;
using System.Diagnostics;
using HAWindowsCompanion.Core.Interfaces;

namespace HAWindowsCompanion.Infrastructure.Platform;

/// <summary>
/// Manages Windows startup behavior using the registry 'Run' key.
/// </summary>
public sealed class StartupManager : IStartupManager
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "HAWindowsCompanion";

    public bool IsStartupEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(AppName) != null;
        }
    }

    public void EnableStartup()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrEmpty(exePath)) return;

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.SetValue(AppName, $"\"{exePath}\"");
    }

    public void DisableStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        key?.DeleteValue(AppName, false);
    }
}
