using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>uikit:Select for the uikit-select and uikit-combobox cases (reference/src/cases/select.rs).</summary>
public static partial class Adapters
{
    /// <summary>The sections of the grouped cases (select.rs GROUPS).</summary>
    private static readonly (string Title, string[] Items)[] SelectGroups =
        [("Fruits", ["Apple", "Banana", "Cherry"]), ("Vegetables", ["Carrot", "Leek", "Potato"])];

    /// <summary>
    /// uikit:Select as GPUI's Select (<paramref name="combobox"/> false) or
    /// Combobox (the combobox class): fruits or the two sections, a disabled
    /// row, the selection (`selected`, or the '1's of `checked`), and the
    /// options the case turns on.
    /// </summary>
    public static global::AvaloniaUIKit.Select SelectCase(GoldenCase c, bool combobox)
    {
        var select = new global::AvaloniaUIKit.Select
        {
            Width = c.Num("width", 200),
            PlaceholderText = c.Str("placeholder", "Select a fruit"),
            IsEnabled = !c.Bool("disabled"),
            IsSearchable = c.Bool("searchable"),
            IsCleanable = c.Bool("cleanable"),
        };
        ClassFrom(select, c, "size", "medium");
        if (combobox)
        {
            select.Classes.Add("combobox");
        }
        FlagClass(select, c, "plain");
        var leaves = new List<string>();
        if (c.Bool("groups"))
        {
            foreach (var (title, items) in SelectGroups)
            {
                var group = new SelectGroup { Title = title };
                foreach (var item in items)
                {
                    group.Items.Add(item);
                    leaves.Add(item);
                }
                select.Items.Add(group);
            }
        }
        else
        {
            foreach (var name in Names((int)c.Num("count", 4)))
            {
                select.Items.Add(name);
                leaves.Add(name);
            }
        }
        if ((int)c.Num("disabled_row", -1) is var disabled and >= 0 && disabled < leaves.Count)
        {
            var name = leaves[disabled];
            select.ItemEnabledSelector = item => !Equals(item, name);
        }
        if (c.Bool("multiple"))
        {
            select.SelectionMode = SelectionMode.Multiple;
        }
        if (c.Has("checked"))
        {
            var bits = c.Str("checked");
            for (var i = 0; i < bits.Length && i < leaves.Count; i++)
            {
                if (bits[i] == '1')
                {
                    select.SelectedItems!.Add(leaves[i]);
                }
            }
        }
        else if (c.Num("selected", -1) is var selected and >= 0)
        {
            select.SelectedIndex = (int)selected;
        }
        if (c.Has("title_prefix"))
        {
            select.TitlePrefix = c.Str("title_prefix");
        }
        if (c.Has("menu_width"))
        {
            select.MenuWidth = c.Num("menu_width", 200);
        }
        if (c.Has("icon"))
        {
            select.Icon = Icon(c.Str("icon")).Data;
        }
        if (c.Has("search_placeholder"))
        {
            select.SearchPlaceholderText = c.Str("search_placeholder");
        }
        if (c.Bool("empty"))
        {
            // The Select story's empty view: "No Data" in a 96px row, muted, at the window's 16px.
            select.EmptyContent = new Panel
            {
                Height = 96,
                Children =
                {
                    new TextBlock
                    {
                        Text = "No Data",
                        FontSize = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = ThemeBrush(c, "UIKit.MutedForeground"),
                    },
                },
            };
        }
        if (c.Bool("footer"))
        {
            // The Combobox story's footer: a full-width ghost button, a plus 8px before its label,
            // in the foreground color (text_color) until the hover's color replaces it.
            var footer = new Button
            {
                Classes = { "ghost" },
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { Icon("plus"), new TextBlock { Text = "New fruit" } },
                },
            };
            footer.Styles.Add(new Style(x => x.OfType<Button>().Not(y => y.Class(":pointerover")).Not(y => y.Class(":pressed")))
            {
                Setters = { new Setter(TemplatedControl.ForegroundProperty, ThemeBrush(c, "UIKit.Foreground")) },
            });
            select.Footer = footer;
        }
        if (c.Bool("trigger"))
        {
            select.TriggerTemplate = StarTrigger(c);
        }
        if (c.Num("max", 0) is var max and > 0)
        {
            // on_will_change keeping at most `max`.
            select.SelectionChanging += (_, e) => e.Cancel = e.Selection.Count > max;
        }
        return select;
    }

    /// <summary>The Combobox story's "Icons" trigger with a star: the icon, the title or the muted placeholder, the caret.</summary>
    private static IDataTemplate StarTrigger(GoldenCase c)
    {
        var caret = c.Str("size", "medium") switch { "xsmall" => 12, "small" => 14, _ => 16 };
        var muted = ThemeBrush(c, "UIKit.MutedForeground");
        return new FuncDataTemplate<SelectTriggerContext>((context, _) =>
        {
            var star = Icon("star");
            star.Width = star.Height = 14;
            star.Foreground = muted;
            var title = new TextBlock { LineHeight = 22.5, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            void Show()
            {
                title.Text = context.Title ?? context.PlaceholderText;
                if (context.HasSelection)
                {
                    title.ClearValue(TextBlock.ForegroundProperty);
                }
                else
                {
                    title.Foreground = muted;
                }
            }
            Show();
            context.PropertyChanged += (_, _) => Show();
            var chevron = Icon("chevron-down");
            chevron.Width = chevron.Height = caret;
            chevron.Foreground = muted;
            Grid.SetColumn(title, 1);
            Grid.SetColumn(chevron, 2);
            return new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                ColumnSpacing = 8,
                Children = { star, title, chevron },
            };
        });
    }
}
