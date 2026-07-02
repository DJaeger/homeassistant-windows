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

    [ObservableProperty] private int _updateInterval = 60;
    [ObservableProperty] private bool _launchAtStartup;

    public SettingsViewModel(ISettingsService settingsService, IStartupManager startupManager, NavigationService navigationService)
    {
        _settingsService = settingsService;
        _startupManager = startupManager;
        _navigationService = navigationService;
        LoadSettings();
    }

    private async void LoadSettings()
    {
        UpdateInterval = await _settingsService.GetAsync<int>("SensorUpdateIntervalSeconds");
        if (UpdateInterval == 0) UpdateInterval = 60;
        LaunchAtStartup = _startupManager.IsStartupEnabled;
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

    [RelayCommand]
    private void NavigateToMain()
    {
        _navigationService.Navigate(typeof(MainPage));
    }
}
