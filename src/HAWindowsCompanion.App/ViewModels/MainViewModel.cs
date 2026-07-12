using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.Views;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Infrastructure.Sensors;

namespace HAWindowsCompanion.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IEnumerable<ISensorProvider> _sensors;
    private readonly ICredentialStore _credentialStore;
    private readonly LocationTrackerService _locationTrackerService;
    private readonly NavigationService _navigationService;
    private readonly IMainWindowCommands _mainWindowCommands;

    [ObservableProperty] private string _connectionStatus = "Disconnected";
    [ObservableProperty] private string _serverUrl = "Not configured";

    public ObservableCollection<SensorInfo> ActiveSensors { get; } = new();

    public MainViewModel(
        IEnumerable<ISensorProvider> sensors,
        ICredentialStore credentialStore,
        LocationTrackerService locationTrackerService,
        NavigationService navigationService,
        IMainWindowCommands mainWindowCommands)
    {
        _sensors = sensors;
        _credentialStore = credentialStore;
        _locationTrackerService = locationTrackerService;
        _navigationService = navigationService;
        _mainWindowCommands = mainWindowCommands;

        _ = LoadStatusAsync(); // Intentionally not awaited because asynchronous work cannot be awaited in the constructor.
    }

    [RelayCommand]
    private void OpenSettings() => _navigationService.Navigate(typeof(SettingsPage));
    public ICommand QuitApplicationCommand => _mainWindowCommands.QuitCommand;


    private async Task LoadStatusAsync()
    {
        try
        {
            var server = await _credentialStore.LoadServerInfoAsync();
            if (server != null)
            {
                ServerUrl = server.InstanceUrl;
                ConnectionStatus = "Connected";
            }

            foreach (var sensor in _sensors)
            {
                if (sensor.IsEnabled)
                {
                    ActiveSensors.Add(new SensorInfo 
                    { 
                        Name = sensor.Name, 
                        Value = sensor.GetCurrentState().State?.ToString() ?? "N/A" 
                    });
                }
            }

            var trackerSnapshot = _locationTrackerService.CurrentStatus;
            ActiveSensors.Add(new SensorInfo
            {
                Name = "Location Tracker",
                Value = BuildLocationValue(trackerSnapshot.LocationName, trackerSnapshot.Attributes),
                Details = BuildLocationDetails(trackerSnapshot.Attributes)
            });
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"Error: {ex.Message}";
        }
    }

    private static string BuildLocationValue(string locationName, Dictionary<string, object> attributes)
    {
        if (attributes is not null && attributes.TryGetValue("gps", out var gpsObj) && gpsObj is double[] gps && gps.Length == 2)
        {
            return $"{locationName} ({gps[0]:F6}, {gps[1]:F6})";
        }

        return locationName;
    }

    private static string BuildLocationDetails(Dictionary<string, object> attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return "Keine Attribute verfügbar";
        }

        var lines = new List<string>();
        foreach (var (key, value) in attributes)
        {
            if (key == "gps" && value is double[] gps && gps.Length == 2)
            {
                lines.Add($"gps: {gps[0]:F6}, {gps[1]:F6}");
            }
            else
            {
                lines.Add($"{key}: {value}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}

public sealed class SensorInfo
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Details { get; set; } = "";
}
