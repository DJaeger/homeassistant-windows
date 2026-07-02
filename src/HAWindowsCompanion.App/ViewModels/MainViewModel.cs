using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.Views;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEnumerable<ISensorProvider> _sensors;
    private readonly ICredentialStore _credentialStore;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private string _connectionStatus = "Disconnected";
    [ObservableProperty] private string _serverUrl = "Not configured";

    public ObservableCollection<SensorInfo> ActiveSensors { get; } = new();
    public ICommand OpenSettingsCommand { get; }

    public MainViewModel(IEnumerable<ISensorProvider> sensors, ICredentialStore credentialStore, NavigationService navigationService)
    {
        _sensors = sensors;
        _credentialStore = credentialStore;
        _navigationService = navigationService;

        OpenSettingsCommand = new RelayCommand(() => _navigationService.Navigate(typeof(SettingsPage)));

        LoadStatus();
    }

    private async void LoadStatus()
    {
        var server = await _credentialStore.LoadServerInfoAsync();
        if (server != null)
        {
            ServerUrl = server.InstanceUrl;
            ConnectionStatus = "Connected";
        }

        foreach (var sensor in _sensors)
        {
            ActiveSensors.Add(new SensorInfo 
            { 
                Name = sensor.Name, 
                Value = sensor.GetCurrentState().State?.ToString() ?? "N/A" 
            });
        }
    }
}

public sealed class SensorInfo
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
