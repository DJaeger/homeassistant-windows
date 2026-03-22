using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.ViewModels;
using HAWindowsCompanion.App.Views;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Infrastructure.Api;
using HAWindowsCompanion.Infrastructure.Authentication;
using HAWindowsCompanion.Infrastructure.Commands;
using HAWindowsCompanion.Infrastructure.Discovery;
using HAWindowsCompanion.Infrastructure.Platform;
using HAWindowsCompanion.Infrastructure.Sensors;

namespace HAWindowsCompanion.App;

/// <summary>
/// Entry point for the WinUI 3 application.
/// Configures dependency injection and hosts background sensor/command services.
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;
    private Window? _window;
    private static readonly Mutex _singleInstanceMutex = new(true, "HAWindowsCompanion_SingleInstance");

    public static IServiceProvider Services => ((App)Current)._host.Services;

    public App()
    {
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
        InitializeComponent();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Platform services
                services.AddSingleton<ICredentialStore, WindowsCredentialStore>();
                services.AddSingleton<ISettingsService, SettingsService>();
                services.AddSingleton<IStartupManager, StartupManager>();

                // Authentication
                services.AddSingleton<IAuthenticationService, OAuth2AuthenticationService>();

                // Discovery
                services.AddSingleton<IDiscoveryService, MdnsDiscoveryService>();

                // HA API client
                services.AddSingleton<IHomeAssistantClient, HomeAssistantApiClient>();
                services.AddHttpClient();

                // Sensors — register all providers
                services.AddSingleton<ISensorProvider, CpuUsageSensor>();
                services.AddSingleton<ISensorProvider, MemoryUsageSensor>();
                services.AddSingleton<ISensorProvider, BatteryStatusSensor>();
                services.AddSingleton<ISensorProvider, ActiveWindowSensor>();
                services.AddSingleton<ISensorProvider, SystemIdleTimeSensor>();
                services.AddSingleton<ISensorProvider, AudioOutputDeviceSensor>();
                services.AddSingleton<ISensorProvider, NetworkSsidSensor>();
                services.AddSingleton<ISensorProvider, LocationSensor>();
                services.AddSingleton<SensorManager>();

                // Notification Service
                services.AddSingleton<INotificationService, ToastNotificationService>();

                // Commands — register all handlers
                services.AddSingleton<ICommandHandler, VolumeCommandHandler>();
                services.AddSingleton<ICommandHandler, LockSessionCommandHandler>();
                services.AddSingleton<ICommandHandler, ShutdownCommandHandler>();
                services.AddSingleton<ICommandHandler, MediaPlayPauseCommandHandler>();
                services.AddSingleton<CommandDispatcher>();

                // ViewModels
                services.AddTransient<SetupWizardViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<MainViewModel>();

                // Navigation
                services.AddSingleton<NavigationService>();
            })
            .Build();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Enforce single instance
        if (!_singleInstanceMutex.WaitOne(TimeSpan.Zero, true))
        {
            // Another instance is already running
            Current.Exit();
            return;
        }

        await _host.StartAsync();

        _window = new MainWindow();

        var settings = Services.GetRequiredService<ISettingsService>();
        var isConfigured = await settings.GetAsync<bool>("IsConfigured");

        if (!isConfigured)
        {
            // Show setup wizard on first run
            if (_window.Content is Microsoft.UI.Xaml.Controls.Frame frame)
            {
                frame.Navigate(typeof(SetupWizardPage));
            }
        }

        _window.Activate();
    }
}
