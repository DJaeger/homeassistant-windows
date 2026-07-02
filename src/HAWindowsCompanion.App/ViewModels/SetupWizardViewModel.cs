using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Core.Models;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.Views;

namespace HAWindowsCompanion.App.ViewModels;

public partial class SetupWizardViewModel : ObservableObject
{
    private readonly IDiscoveryService _discoveryService;
    private readonly IAuthenticationService _authService;
    private readonly IHomeAssistantClient _haClient;
    private readonly ICredentialStore _credentialStore;
    private readonly ISettingsService _settingsService;
    private readonly NavigationService _navigationService;

    [ObservableProperty] private int _currentStep = 0;
    [ObservableProperty] private bool _isScanning = false;
    [ObservableProperty] private string _customInstanceUrl = "";
    [ObservableProperty] private bool _isConnecting = false;
    [ObservableProperty] private string? _errorMessage;

    // Computed property for Border.IsHitTestVisible binding
    public bool IsNotConnecting => !IsConnecting;

    public ObservableCollection<DiscoveredInstance> DiscoveredInstances { get; } = new();

    public SetupWizardViewModel(
        IDiscoveryService discoveryService,
        IAuthenticationService authService,
        IHomeAssistantClient haClient,
        ICredentialStore credentialStore,
        ISettingsService settingsService,
        NavigationService navigationService)
    {
        _discoveryService = discoveryService;
        _authService = authService;
        _haClient = haClient;
        _credentialStore = credentialStore;
        _settingsService = settingsService;
        _navigationService = navigationService;
    }

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
                DeviceId = Guid.NewGuid().ToString(),
                AppVersion = System.Reflection.Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                    .Cast<System.Reflection.AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault()?.InformationalVersion ?? "2026.3.0",
                DeviceName = Environment.MachineName,
                Manufacturer = "FaserF",
                Model = "Windows PC",
                OsVersion = Environment.OSVersion.VersionString
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
