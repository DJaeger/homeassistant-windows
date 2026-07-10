using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.ViewModels;
using HAWindowsCompanion.App.Views;
using HAWindowsCompanion.Core.Interfaces;
using HAWindowsCompanion.Infrastructure.Api;
using HAWindowsCompanion.Infrastructure.Authentication;
using HAWindowsCompanion.Infrastructure.Commands;
using HAWindowsCompanion.Infrastructure.Discovery;
using HAWindowsCompanion.Infrastructure.Logging;
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
    public static MainWindow? MainWindow => ((App)Current)._window as MainWindow;

    public App()
    {
        Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);
        InitializeComponent();

        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging((context, logging) =>
            {
                // Check if file logging is enabled (DI-friendly approach)
                var tempServices = new ServiceCollection();
                tempServices.AddSingleton<ISettingsService, SettingsService>();
                var tempProvider = tempServices.BuildServiceProvider();
                var settingsService = tempProvider.GetRequiredService<ISettingsService>();

                var isFileLoggingEnabled = settingsService.GetAsync<bool>("IsFileLoggingEnabled").GetAwaiter().GetResult();

                if (isFileLoggingEnabled)
                {
                    var options = new FileLoggerOptions();
                    logging.AddProvider(new FileLoggerProvider(options));
                }

                // Increase log level for debug builds
#if DEBUG
                logging.SetMinimumLevel(LogLevel.Debug);
#else
                logging.SetMinimumLevel(LogLevel.Information);
#endif
            })
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
                services.AddSingleton<IZonesService, ZonesService>();
                services.AddHttpClient();

                // Sensors — register all providers
                services.AddSingleton<ISensorProvider, CpuUsageSensor>();
                services.AddSingleton<ISensorProvider, MemoryUsageSensor>();
                services.AddSingleton<ISensorProvider, BatteryStatusSensor>();
                services.AddSingleton<ISensorProvider, ActiveWindowSensor>();
                services.AddSingleton<ISensorProvider, SystemIdleTimeSensor>();
                services.AddSingleton<ISensorProvider, AudioOutputDeviceSensor>();
                services.AddSingleton<ISensorProvider, NetworkSsidSensor>();
                services.AddHostedService<SensorManager>();
                services.AddSingleton<LocationTrackerService>();
                services.AddHostedService(provider => provider.GetRequiredService<LocationTrackerService>());

                // Notification Service
                services.AddSingleton<INotificationService, ToastNotificationService>();

                // Commands — register all handlers
                services.AddSingleton<ICommandHandler, VolumeCommandHandler>();
                services.AddSingleton<ICommandHandler, LockSessionCommandHandler>();
                services.AddSingleton<ICommandHandler, ShutdownCommandHandler>();
                services.AddSingleton<ICommandHandler, MediaPlayPauseCommandHandler>();
                services.AddHostedService<CommandDispatcher>();

                // ViewModels
                services.AddTransient<SetupWizardViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<MainViewModel>();

                // Views
                services.AddTransient<MainPage>();
                services.AddTransient<SettingsPage>();
                services.AddTransient<SetupWizardPage>();

                // Navigation
                services.AddSingleton<NavigationService>();

                // MainWindow
                services.AddSingleton<MainWindow>();

                // Register MainWindow Commands Interface
                services.AddSingleton<IMainWindowCommands>(sp => sp.GetRequiredService<MainWindow>());
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

        _window = Services.GetRequiredService<MainWindow>();

        var settings = Services.GetRequiredService<ISettingsService>();
        var isConfigured = await settings.GetAsync<bool>("IsConfigured");
        var navigationService = Services.GetRequiredService<NavigationService>();

        if (!isConfigured)
        {
            // Show setup wizard on first run
            navigationService.Navigate(typeof(SetupWizardPage));
        }
        else
        {
            // Show main page if already configured
            navigationService.Navigate(typeof(MainPage));
        }

        _window.Activate();
    }
}
