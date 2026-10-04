using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIKit.Tests.Golden;
using Tabalonia.Controls;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for the third-party libraries' themes (ADR 28).</summary>
public static partial class Adapters
{
    /// <summary>
    /// The tabalonia cases (reference/src/cases/tabs.rs with equal tab widths) as
    /// Tabalonia's TabsControl: the same tabs as DragTabItems, TabItemWidth for
    /// the tabs' one width, and the bar's prefix and suffix as Left/RightContent.
    /// </summary>
    public static TabsControl TabaloniaCase(GoldenCase c)
    {
        var tabs = new TabsControl
        {
            Width = c.Num("width", 360),
            HorizontalAlignment = HorizontalAlignment.Left,
            TabItemWidth = c.Params["tab_widths"] is System.Text.Json.Nodes.JsonArray widths ? widths[0]!.GetValue<double>() : 100,
            EnableTabDetaching = false,
            ShowDefaultAddButton = false,
            ShowDefaultCloseButton = c.Bool("closable"),
        };
        TabClasses(tabs, c);
        FlagClass(tabs, c, "menu");
        if (c.Bool("bar_prefix"))
        {
            tabs.LeftContent = BarButtons("chevron-left", "chevron-right");
        }
        if (c.Bool("bar_suffix"))
        {
            tabs.RightContent = BarButtons("inbox", "ellipsis");
        }
        var items = new ObservableCollection<object>();
        for (var i = 0; i < TabContents.Length; i++)
        {
            var item = new DragTabItem();
            TabItemLook(item, c, i);
            item.ClearValue(Layoutable.WidthProperty);
            item.Header = c.Bool("icons") ? Icon(TabContents[i].Icon) : TabContents[i].Label;
            items.Add(item);
        }
        tabs.ItemsSource = items;
        tabs.SelectedIndex = (int)c.Num("selected", 0);
        return tabs;
    }

    /// <summary>The tabs story's prefix and suffix: two ghost xsmall icon buttons in an mx_1 row.</summary>
    private static StackPanel BarButtons(string first, string second)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0) };
        foreach (var name in new[] { first, second })
        {
            var button = new Button { Content = Icon(name) };
            button.Classes.AddRange(["ghost", "xsmall", "icon-only"]);
            ((PathIcon)button.Content).Classes.Add("xsmall");
            row.Children.Add(button);
        }
        return row;
    }
}
