using System.Diagnostics;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.Views;

namespace HAWindowsCompanion.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupManager _startupManager;
    private readonly NavigationService _navigationService;
    private readonly ICommand _restartCommand;

    [ObservableProperty] public partial int UpdateInterval { get; set; } = 60;
    [ObservableProperty] public partial bool LaunchAtStartup { get; set; }
    [ObservableProperty] public partial bool IsFileLoggingEnabled { get; set; }
    [ObservableProperty] public partial bool IsRestartRequired { get; set; }

    public SettingsViewModel(
        ISettingsService settingsService, 
        IStartupManager startupManager, 
        NavigationService navigationService,
        IMainWindowCommands mainWindowCommands)
    {
        _settingsService = settingsService;
        _startupManager = startupManager;
        _navigationService = navigationService;
        _restartCommand = mainWindowCommands.RestartCommand;
        LoadSettings();
    }

    public ICommand RestartApplicationCommand => _restartCommand;

    private async void LoadSettings()
    {
        UpdateInterval = await _settingsService.GetAsync<int>("SensorUpdateIntervalSeconds");
        if (UpdateInterval == 0) UpdateInterval = 60;
        LaunchAtStartup = _startupManager.IsStartupEnabled;

        // Load file logging setting
        IsFileLoggingEnabled = await _settingsService.GetAsync<bool>("IsFileLoggingEnabled");

        // Restart is not required initially
        IsRestartRequired = false;
    }

    [RelayCommand]
    private void ResetSettings()
    {
        var wasFileLoggingEnabled = IsFileLoggingEnabled;
        _settingsService.Reset();
        _startupManager.DisableStartup();
        if (wasFileLoggingEnabled)
            _restartCommand.Execute(null);
        else
        {
            LoadSettings();
            _navigationService.Navigate(typeof(SetupWizardPage));
        }
    }

    partial void OnUpdateIntervalChanged(int value)
    {
        _settingsService.SetAsync("SensorUpdateIntervalSeconds", value);
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        if (value) _startupManager.EnableStartup();
        else _startupManager.DisableStartup();
    }

    partial void OnIsFileLoggingEnabledChanged(bool value)
    {
        // Show restart button
        IsRestartRequired = true;

        // Persist immediately (fire-and-forget like existing pattern)
        _ = _settingsService.SetAsync("IsFileLoggingEnabled", value);
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HAWindowsCompanion",
            "logs");

        try
        {
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = logPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception)
        {
            // Silent fail - opening log folder is not critical
        }
    }

    [RelayCommand]
    private void NavigateToMain()
    {
        _navigationService.Navigate(typeof(MainPage));
    }

    [RelayCommand]
    private void NavigateToSetupWizard()
    {
        _navigationService.Navigate(typeof(SetupWizardPage));
    }
}
