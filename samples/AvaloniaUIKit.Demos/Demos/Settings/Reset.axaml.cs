using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class SettingsReset
{
    // A custom field the item cannot follow: the demo says when it differs from
    // Comfortable, and puts Comfortable back when the page resets it.
    private void OnDensityClick(object? sender, RoutedEventArgs e) => Choose((Button)sender!);

    private void OnDensityReset(object? sender, RoutedEventArgs e) => Choose(this.GetControl<Button>("Comfortable"));

    private void Choose(Button chosen)
    {
        foreach (var name in new[] { "Comfortable", "Compact" })
        {
            var button = this.GetControl<Button>(name);
            button.Classes.Set("primary", button == chosen);
            button.Classes.Set("outline", button != chosen);
        }
        this.GetControl<SettingItem>("Density").IsModified = chosen.Name != "Comfortable";
    }
}
