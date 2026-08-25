using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using HAWindowsCompanion.App.Services;
using HAWindowsCompanion.App.Views;

namespace HAWindowsCompanion.App;

/// <summary>
/// Main application window with system tray integration.
/// Minimizes to tray on close, stays running in the background.
/// </summary>
public sealed partial class MainWindow : Window, IMainWindowCommands
{
    private const int MinWidth = 480;
    private const int MinHeight = 640;

    private readonly NavigationService _navigationService;
    private IntPtr _hwnd;
    private IntPtr _oldWndProc;
    private WndProc? _newWndProc;

    public ICommand ShowWindowCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand QuitCommand { get; }
    public ICommand RestartCommand { get; }

    public MainWindow(NavigationService navigationService)
    {
        _navigationService = navigationService;

        ShowWindowCommand = new RelayCommand(ShowWindow);
        OpenSettingsCommand = new RelayCommand(NavigateToSettings);
        QuitCommand = new RelayCommand(QuitApplication);
        RestartCommand = new RelayCommand(RestartApplication);

        InitializeComponent();

        // Initialize NavigationService with ContentFrame
        _navigationService.Initialize(ContentFrame);

        Title = "Home Assistant Companion";
        ExtendsContentIntoTitleBar = true;

        // Configure window presenter and maximize on startup
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMinimizable = true;
        }

        // Minimize to tray on close instead of exiting
        AppWindow.Closing += OnClosing;

        // Install Win32 window procedure to enforce minimum window size
        _hwnd = WindowNative.GetWindowHandle(this);
        _newWndProc = new WndProc(WindowProc);
        _oldWndProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
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

    internal void NavigateToMainPage()
    {
        _navigationService.Navigate(typeof(MainPage));
    }

    internal void NavigateToSetupWizard()
    {
        _navigationService.Navigate(typeof(SetupWizardPage));
    }

    internal void NavigateToSettings()
    {
        ShowWindow();
        _navigationService.Navigate(typeof(SettingsPage));
    }

    private void QuitApplication()
    {
        // Clean shutdown
        AppWindow.Closing -= OnClosing;
        Close();
        Application.Current.Exit();
    }

    private void RestartApplication()
    {
        try
        {
            // Start new instance
            Process.Start(new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot determine executable path"),
                UseShellExecute = true
            });

            // Clean shutdown of current instance
            QuitApplication();
        }
        catch
        {
            // Fallback: quit only
            QuitApplication();
        }
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            // Get DPI scaling factor
            uint dpi = GetDpiForWindow(hWnd);
            double scaleFactor = dpi / 96.0; // 96 DPI is 100% scaling

            // Marshal MINMAXINFO structure from lParam
            MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);

            // Set minimum tracking size with DPI scaling
            minMaxInfo.ptMinTrackSize.X = (int)(MinWidth * scaleFactor);
            minMaxInfo.ptMinTrackSize.Y = (int)(MinHeight * scaleFactor);

            // Marshal back to lParam
            Marshal.StructureToPtr(minMaxInfo, lParam, true);

            return IntPtr.Zero;
        }

        // Call original window procedure for all other messages
        return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
    }

    #region Win32 Interop

    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    private const uint WM_GETMINMAXINFO = 0x0024;
    private const int GWLP_WNDPROC = -4;

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        => IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        => IntPtr.Size == 8 ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    #endregion
}
