using System.Collections.ObjectModel;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.Views;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.App.ViewModels;

public partial class SetupWizardViewModel : ObservableObject
{
    private readonly IDiscoveryService _discoveryService;
    private readonly IAuthenticationService _authService;
    private readonly IHomeAssistantClient _haClient;
    private readonly ICredentialStore _credentialStore;
    private readonly ISettingsService _settingsService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] public partial int CurrentStep { get; set; } = 0;
    [ObservableProperty] public partial bool IsScanning { get; set; } = false;
    [ObservableProperty] public partial string CustomInstanceUrl { get; set; } = "";
    [ObservableProperty] public partial bool IsConnecting { get; set; } = false;
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial bool IsConfigured { get; set; } = false;

    public SetupWizardViewModel(
        IDiscoveryService discoveryService,
        IAuthenticationService authService,
        IHomeAssistantClient haClient,
        ICredentialStore credentialStore,
        ISettingsService settingsService,
        NavigationService navigationService
    )
    {
        _discoveryService = discoveryService;
        _authService = authService;
        _haClient = haClient;
        _credentialStore = credentialStore;
        _settingsService = settingsService;
        _navigationService = navigationService;
        LoadSettings();
    }

    private async void LoadSettings()
    {
        IsConfigured = await _settingsService.GetAsync<bool>("IsConfigured");
    }


    // Computed property for Border.IsHitTestVisible binding
    public bool IsNotConnecting => !IsConnecting;

    public ObservableCollection<DiscoveredInstance> DiscoveredInstances { get; } = new();

    // Notify IsNotConnecting when IsConnecting changes
    partial void OnIsConnectingChanged(bool value)
    {
        OnPropertyChanged(nameof(IsNotConnecting));
    }

    [RelayCommand]
    private async Task ScanInstancesAsync()
    {
        IsScanning = true;
        ErrorMessage = null;
        DiscoveredInstances.Clear();

        try
        {
            var instances = await _discoveryService.DiscoverInstancesAsync(TimeSpan.FromSeconds(5));
            foreach (var instance in instances)
            {
                DiscoveredInstances.Add(instance);
            }

            if (DiscoveredInstances.Count == 0)
            {
                ErrorMessage = "No instances found. Please enter URL manually.";
            }
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task ConnectInstanceAsync(DiscoveredInstance? instance)
    {
        string url = instance?.Url ?? CustomInstanceUrl;
        if (string.IsNullOrEmpty(url)) return;

        // Get manufacturer and model using WMI
        string manufacturer = "N/A";
        string model = "N/A";
        ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_ComputerSystem");
        foreach (ManagementObject obj in searcher.Get())
        {
            manufacturer = obj["Manufacturer"]?.ToString() ?? "N/A";
            model = obj["Model"]?.ToString() ?? "N/A";
        }

        // If DeviceId is not set, generate a new one and save it
        string? DeviceId = await _settingsService.GetAsync<string>("DeviceId");
        if (DeviceId == null)
        {
            DeviceId = Guid.NewGuid().ToString();
            await _settingsService.SetAsync("DeviceId", DeviceId);
        }

        IsConnecting = true;
        ErrorMessage = null;

        try
        {
            // 1. Authenticate
            var code = await _authService.AuthorizeAsync(url);
            var tokens = await _authService.ExchangeCodeAsync(url, code);

            // 2. Register Device
            var registration = new DeviceRegistration
            {
                DeviceId = DeviceId,
                AppVersion = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .Cast<System.Reflection.AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault()?.InformationalVersion ?? "2026.3.0",
                DeviceName = System.Net.Dns.GetHostName(),
                Manufacturer = manufacturer,
                Model = model,
                OsVersion = Environment.OSVersion.VersionString,
                AppData = new Dictionary<string, object>() {
                    ["push_websocket_channel"] = true
                }
            };

            var serverInfo = await _haClient.RegisterDeviceAsync(url, tokens.AccessToken, registration);

            // 3. Persist everything
            await _credentialStore.SaveTokenAsync(tokens);
            await _credentialStore.SaveServerInfoAsync(serverInfo);
            await _settingsService.SetAsync("IsConfigured", true);
            await _settingsService.SetAsync("SensorUpdateIntervalSeconds", 60);

            // 4. Move to success/main
            _navigationService.ClearBackStack();
            _navigationService.Navigate(typeof(MainPage));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _navigationService.Navigate(typeof(SettingsPage));
    }

}
