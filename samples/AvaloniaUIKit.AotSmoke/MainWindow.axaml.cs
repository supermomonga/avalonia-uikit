using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;
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
        // uikit:NotificationList's stack, with an action and an icon of the app's.
        Opened += (_, _) =>
        {
            var list = new NotificationList(this) { Placement = NotificationPlacement.BottomRight };
            list.Show(new NotificationItem(null, "Every uikit control is loaded.") { Icon = IconName.Bell, AutoHide = false });
            list.Show(new NotificationItem(NotificationType.Success, "The stack is ready.")
            {
                Title = "NotificationList",
                Action = new Button { Content = "Close", Classes = { "primary" }, [NotificationCard.CloseOnClickProperty] = true },
            });
        };
        // GPUI's scroll_to_item on a ListBox, waiting for the first layout.
        this.FindControl<ListBox>("Fruits")!.ScrollToItem(2, ScrollStrategy.Center);
        // A sheet shown and closed again, so its template runs too.
        Opened += (_, _) =>
        {
            var sheet = NewSheet();
            sheet.Show(this);
            sheet.Close();
        };
    }

    private void OnOpenSheet(object? sender, RoutedEventArgs e) => NewSheet().Show(this);

    private static Sheet NewSheet() => new()
    {
        Title = "Settings",
        Content = new StackPanel
        {
            Spacing = 12,
            Children = { new TextBox { PlaceholderText = "Your name" }, new CheckBox { Content = "Notify me" } },
        },
        Footer = new Button { Content = "Save", Classes = { "primary" } },
    };

    private void OnPreviousSlide(object? sender, RoutedEventArgs e) => this.FindControl<Carousel>("Slides")!.Previous();

    private void OnNextSlide(object? sender, RoutedEventArgs e) => this.FindControl<Carousel>("Slides")!.Next();

    // ButtonGroup::on_click: the app applies the selection the click makes.
    private void OnButtonGroupClick(object? sender, ButtonGroupClickEventArgs e)
    {
        var group = (ButtonGroup)sender!;
        for (var i = 0; i < group.Children.Count; i++)
        {
            group.Children[i].Classes.Set("selected", e.SelectedIndices.Contains(i));
        }
    }
}
