using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using HAWindowsCompanion.App.Views;

namespace HAWindowsCompanion.App;

/// <summary>
/// Main application window with system tray integration.
/// Minimizes to tray on close, stays running in the background.
/// </summary>
public sealed partial class MainWindow : Window
{
    public ICommand ShowWindowCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand QuitCommand { get; }

    public MainWindow()
    {
        ShowWindowCommand = new RelayCommand(ShowWindow);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        QuitCommand = new RelayCommand(QuitApplication);

        InitializeComponent();

        Title = "Home Assistant Companion";
        ExtendsContentIntoTitleBar = true;

        // Minimize to tray on close instead of exiting
        AppWindow.Closing += OnClosing;

        // Navigate to main page by default
        ContentFrame.Navigate(typeof(MainPage));
    }

    private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Prevent actual close — hide to tray instead
        args.Cancel = true;
        AppWindow.Hide();
    }

    private void ShowWindow()
    {
        AppWindow.Show();
        // Bring to front
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Restore();
        }
    }

    private void OpenSettings()
    {
        ShowWindow();
        ContentFrame.Navigate(typeof(SettingsPage));
    }

    private void QuitApplication()
    {
        // Clean shutdown
        AppWindow.Closing -= OnClosing;
        Close();
        Application.Current.Exit();
    }
}
