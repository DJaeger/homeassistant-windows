using System.Collections.ObjectModel;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.Views;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;

namespace HAWindowsCompanion.App.ViewModels;

public partial class SetupWizardViewModel(
        IDiscoveryService _discoveryService,
        IAuthenticationService _authService,
        IHomeAssistantClient _haClient,
        ICredentialStore _credentialStore,
        ISettingsService _settingsService,
        NavigationService _navigationService
) : ObservableObject
{
    [ObservableProperty] private int _currentStep = 0;
    [ObservableProperty] private bool _isScanning = false;
    [ObservableProperty] private string _customInstanceUrl = "";
    [ObservableProperty] private bool _isConnecting = false;
    [ObservableProperty] private string? _errorMessage;

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
}
