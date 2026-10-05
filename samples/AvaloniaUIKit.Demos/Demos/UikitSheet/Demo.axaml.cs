using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class UikitSheetDemo
{
    private void OnShowSheet(object? sender, RoutedEventArgs e)
    {
        var sheet = NewSheet();
        sheet.Placement = Enum.Parse<DrawerPlacement>((string)((Button)sender!).Tag!);
        sheet.Size = new RelativeScalar(sheet.Placement is DrawerPlacement.Top or DrawerPlacement.Bottom ? 200 : 300, RelativeUnit.Absolute);
        sheet.Show(this);
    }

    private void OnShowHalf(object? sender, RoutedEventArgs e)
    {
        var sheet = NewSheet();
        sheet.Size = RelativeScalar.Parse("50%");
        sheet.Show(this);
    }

    // A sheet with a title, a body and a footer whose buttons close it.
    private static Sheet NewSheet()
    {
        var sheet = new Sheet { Title = "Edit profile" };
        var save = new Button { Classes = { "primary" }, Content = "Save changes" };
        var cancel = new Button { Content = "Cancel" };
        save.Click += (_, _) => sheet.Close();
        cancel.Click += (_, _) => sheet.Close();
        sheet.Content = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "Make changes to your profile here.", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new TextBox { Text = "Jason Lee" },
                new TextBox { Text = "@huacnlee" },
            },
        };
        sheet.Footer = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Children = { save, cancel } };
        return sheet;
    }
}
