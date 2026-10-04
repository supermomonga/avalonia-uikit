using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class ComboboxFooter
{
    // The footer adds what was typed in the search field and selects it.
    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        var select = this.FindControl<Select>("Universities")!;
        var name = select.SearchText.Trim();
        if (name.Length == 0)
        {
            return;
        }
        if (!select.Items.Contains(name))
        {
            select.Items.Add(name);
        }
        select.SelectedItem = name;
        select.IsDropDownOpen = false;
    }
}
