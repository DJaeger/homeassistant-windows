using System;
using Microsoft.UI.Xaml.Controls;

namespace HAWindowsCompanion.App.Services;

/// <summary>
/// Simple navigation service to allow ViewModels to trigger page changes.
/// </summary>
public sealed class NavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame)
    {
        _frame = frame;
    }

    public void Navigate(Type pageType, object? parameter = null)
    {
        _frame?.Navigate(pageType, parameter);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
