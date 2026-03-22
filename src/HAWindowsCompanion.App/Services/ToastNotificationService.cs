using Microsoft.Toolkit.Uwp.Notifications;
using HAWindowsCompanion.Core.Interfaces;

namespace HAWindowsCompanion.App.Services;

public class ToastNotificationService : INotificationService
{
    public void ShowNotification(string title, string message, string? imageUrl = null)
    {
        var builder = new ToastContentBuilder()
            .AddText(title)
            .AddText(message);

        if (!string.IsNullOrEmpty(imageUrl))
        {
            builder.AddInlineImage(new Uri(imageUrl));
        }

        builder.Show();
    }
}
