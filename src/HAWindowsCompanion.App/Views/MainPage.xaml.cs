using Microsoft.UI.Xaml.Controls;
using HAWindowsCompanion.App.ViewModels;

namespace HAWindowsCompanion.App.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
