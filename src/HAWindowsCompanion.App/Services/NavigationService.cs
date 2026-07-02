using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace HAWindowsCompanion.App.Services;

/// <summary>
/// Navigation service to allow ViewModels to trigger page changes with transitions and error handling.
/// Resolves pages from the dependency injection container to support constructor injection.
/// </summary>
public sealed class NavigationService
{
    private Frame? _frame;
    private readonly IServiceProvider _serviceProvider;

    public event EventHandler<Type>? Navigated;
    public event EventHandler<string>? NavigationFailed;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Initialize(Frame frame)
    {
        _frame = frame;

        if (_frame != null)
        {
            _frame.Navigated += OnFrameNavigated;
            _frame.NavigationFailed += OnFrameNavigationFailed;
        }
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        Navigated?.Invoke(this, e.SourcePageType);
    }

    private void OnFrameNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        NavigationFailed?.Invoke(this, e.Exception.Message);
    }

    public bool Navigate(Type pageType, object? parameter = null, bool addTransition = true)
    {
        if (_frame == null)
            return false;

        try
        {
            // Resolve page from DI container instead of using Frame.Navigate(Type)
            // This allows pages to use constructor dependency injection
            var page = _serviceProvider.GetRequiredService(pageType) as Page;
            if (page == null)
                return false;

            // Navigate by setting content directly since we already have the resolved instance
            _frame.Content = page;
            return true;
        }
        catch (Exception ex)
        {
            NavigationFailed?.Invoke(this, ex.Message);
            return false;
        }
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }

    /// <summary>
    /// Clears the navigation back stack. Useful after completing setup wizard to prevent going back.
    /// </summary>
    public void ClearBackStack()
    {
        _frame?.BackStack.Clear();
    }

    /// <summary>
    /// Gets whether the Frame can navigate backwards.
    /// </summary>
    public bool CanGoBack => _frame?.CanGoBack ?? false;
}
