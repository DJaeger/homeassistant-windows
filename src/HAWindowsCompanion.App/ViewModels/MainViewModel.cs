using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEnumerable<ISensorProvider> _sensors;
    private readonly ICredentialStore _credentialStore;

    [ObservableProperty] private string _connectionStatus = "Disconnected";
    [ObservableProperty] private string _serverUrl = "Not configured";

    public ObservableCollection<SensorInfo> ActiveSensors { get; } = new();

    public MainViewModel(IEnumerable<ISensorProvider> sensors, ICredentialStore credentialStore)
    {
        _sensors = sensors;
        _credentialStore = credentialStore;
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
