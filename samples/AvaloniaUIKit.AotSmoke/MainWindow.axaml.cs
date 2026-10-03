using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;

namespace AvaloniaUIKit.AotSmoke;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        // A notification card, so its theme (converters and animations) runs too.
        Opened += (_, _) => new WindowNotificationManager(this) { MaxItems = 10 }
            .Show(new Notification("Gallery", "Every theme is loaded."), NotificationType.Information, TimeSpan.Zero);
    }
}
