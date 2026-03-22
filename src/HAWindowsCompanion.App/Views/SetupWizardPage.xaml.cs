using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using HAWindowsCompanion.App.ViewModels;

namespace HAWindowsCompanion.App.Views;

public sealed partial class SetupWizardPage : Page
{
    public SetupWizardViewModel ViewModel { get; }

    public SetupWizardPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SetupWizardViewModel>();
    }
}
