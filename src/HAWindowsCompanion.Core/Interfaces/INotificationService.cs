namespace HAWindowsCompanion.Core.Interfaces;

/// <summary>
/// Service for showing system notifications to the user.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a toast notification.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification body text.</param>
    /// <param name="imageUrl">Optional URL to an image to display in the notification.</param>
    void ShowNotification(string title, string message, string? imageUrl = null);
}
