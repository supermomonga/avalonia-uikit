using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for uikit:ListView and uikit:Tree (reference/src/cases/list.rs, tree.rs).</summary>
public static partial class Adapters
{
    /// <summary>A row of the confirmed case: the name, and whether the row is confirmed.</summary>
    public sealed record ListFruit(string Name, bool IsConfirmed)
    {
        public override string ToString() => Name;
    }

    /// <summary>The ListView cases' sections: the middle one is empty; the last gets `extra` more rows.</summary>
    public static ListSection[] ListSections(int extra = 0, bool footers = true)
    {
        var citrus = new ListSection { Header = "Citrus", Items = { "Lemon", "Orange", "Lime" } };
        for (var i = 0; i < extra; i++)
        {
            citrus.Items.Add($"Item {i + 4}");
        }
        ListSection[] sections =
        [
            new() { Header = "Fruits", Items = { "Apple", "Banana", "Cherry" } },
            new() { Header = "Berries" },
            citrus,
        ];
        foreach (var section in sections.Where(_ => footers))
        {
            section.Footer = $"{section.Items.Count} items";
        }
        return sections;
    }

    /// <summary>
    /// The uikit-list cases as a ListView: the fruit names (or sections), a
    /// selected and a disabled row, the search field and a query, loading, a
    /// confirmed row with the check icon, and a scroll to a row.
    /// </summary>
    public static ListView ListViewCase(GoldenCase c)
    {
        var list = new ListView
        {
            Width = c.Num("width", 240),
            Height = c.Num("height", 200),
            IsSearchable = c.Bool("searchable"),
            IsSelectable = !c.Has("selectable") || c.Bool("selectable"),
            IsLoading = c.Bool("loading"),
        };
        if (c.Str("scrollbar_mode", "hover") == "always")
        {
            ScrollViewer.SetAllowAutoHide(list, false);
        }
        var strategy = c.Str("strategy", "top") switch
        {
            "center" => ScrollStrategy.Center,
            "bottom" => ScrollStrategy.Bottom,
            _ => ScrollStrategy.Top,
        };
        if (c.Bool("sections"))
        {
            var rowHeaders = c.Bool("row_headers");
            list.ItemsSource = ListSections((int)c.Num("extra", 0), footers: !rowHeaders);
            if (rowHeaders)
            {
                // An app's header as tall as a row: 16px muted text with the row's padding.
                list.SectionHeaderTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<ListSection>((section, _) => new TextBlock
                {
                    Text = section.Header as string,
                    FontSize = 16,
                    LineHeight = 26,
                    Margin = new Thickness(12, 4),
                    Foreground = ThemeBrush(c, "UIKit.MutedForeground"),
                });
            }
            if (IndexPathParam(c, "selected") is { } selected)
            {
                list.SelectedIndex = selected;
            }
            if (IndexPathParam(c, "scroll_to") is { } target)
            {
                list.ScrollToItem(target, strategy);
            }
            return list;
        }

        var names = c.Bool("empty") ? [] : Names((int)c.Num("count", 5)).ToList();
        if (c.Bool("check_icon"))
        {
            var confirmed = (int)c.Num("confirmed", -1);
            list.ItemsSource = names.Select((name, i) => new ListFruit(name, i == confirmed)).ToList();
            // An app's row style: the check icon on every row, confirmed from the item.
            list.Styles.Add(new Style(x => x.OfType<ListItem>())
            {
                Setters =
                {
                    new Setter(ListItem.CheckIconProperty, Icon("check").Data),
                    new Setter(ListItem.IsConfirmedProperty, Avalonia.Data.CompiledBinding.Create<ListFruit, bool>(f => f.IsConfirmed)),
                },
            });
        }
        else
        {
            list.ItemsSource = names;
        }
        if (c.Num("disabled_row", -1) is var disabled and >= 0)
        {
            var name = names[(int)disabled];
            list.DisabledSelector = item => item?.ToString() == name;
        }
        if (c.Num("selected", -1) is var row and >= 0)
        {
            list.SelectedIndex = new IndexPath(0, (int)row);
        }
        if (c.Num("scroll_to", -1) is var scrollTo and >= 0)
        {
            list.ScrollToItem(new IndexPath(0, (int)scrollTo), strategy);
        }
        if (c.Has("query"))
        {
            // As GPUI's case, the query comes once the list has its rows.
            var query = c.Str("query");
            list.Loaded += (_, _) => list.SearchText = query;
        }
        return list;
    }

    private static IndexPath? IndexPathParam(GoldenCase c, string key) =>
        c.Params[key] is System.Text.Json.Nodes.JsonArray pair
            ? new IndexPath(pair[0]!.GetValue<int>(), pair[1]!.GetValue<int>())
            : null;

    /// <summary>A TreeItem with children, registered under its path.</summary>
    private static TreeItem Node(Dictionary<string, TreeItem> byId, string id, bool expanded = false, bool disabled = false, params TreeItem[] children)
    {
        var item = new TreeItem { Label = id[(id.LastIndexOf('/') + 1)..], IsExpanded = expanded, IsDisabled = disabled };
        foreach (var child in children)
        {
            item.Children.Add(child);
        }
        byId[id] = item;
        return item;
    }

    /// <summary>The trees of tree.rs: the sample, a deeper one and a big one.</summary>
    public static (TreeItem[] Roots, Dictionary<string, TreeItem> ById) TreeItems(string kind, int extra = 0)
    {
        var byId = new Dictionary<string, TreeItem>();
        TreeItem N(string id, bool expanded = false, bool disabled = false, params TreeItem[] children) =>
            Node(byId, id, expanded, disabled, children);
        TreeItem[] roots = kind switch
        {
            "deep" =>
            [
                N("src", true, false,
                    N("src/components", false, false, N("src/components/button.rs"), N("src/components/list.rs")),
                    N("src/ui", false, false,
                        N("src/ui/widgets", false, false,
                            [N("src/ui/widgets/input.rs"), N("src/ui/widgets/select.rs"), .. Enumerable.Range(0, extra).Select(i => N($"src/ui/widgets/widget_{i:00}.rs"))]),
                        N("src/ui/theme.rs")),
                    N("src/lib.rs")),
                N("docs", false, false, N("docs/guide.md")),
                N("Cargo.toml"),
            ],
            "big" =>
            [
                N("src", true, false, [.. Enumerable.Range(0, 200).Select(i => N($"src/file_{i:000}.rs"))]),
                N("Cargo.toml"),
            ],
            _ =>
            [
                N("src", true, false,
                    N("src/components", true, false, N("src/components/button.rs"), N("src/components/tree.rs")),
                    N("src/lib.rs")),
                N("assets", false, true, N("assets/logo.svg")),
                N("Cargo.toml"),
            ],
        };
        return (roots, byId);
    }

    /// <summary>
    /// The uikit-tree cases as a Tree of TreeItems; `selected` is a row,
    /// `reveal` an item's path, revealed by `strategy` and selected.
    /// </summary>
    public static Tree TreeCase(GoldenCase c)
    {
        var (roots, byId) = TreeItems(c.Str("items", "sample"), (int)c.Num("extra", 0));
        var tree = new Tree { Width = c.Num("width", 240), Height = c.Num("height", 238), ItemsSource = roots };
        if (c.Str("scrollbar_mode", "hover") == "always")
        {
            ScrollViewer.SetAllowAutoHide(tree, false);
        }
        if (c.Num("selected", -1) is var selected and >= 0)
        {
            tree.SelectedIndex = (int)selected;
        }
        if (c.Bool("rounded"))
        {
            tree.Padding = new Thickness(4);
            tree.BorderThickness = new Thickness(1);
            tree.BorderBrush = ThemeBrush(c, "UIKit.Border");
            tree.CornerRadius = new CornerRadius(5.5);
            tree.Styles.Add(new Style(x => x.OfType<ListItem>())
            {
                Setters = { new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(6)) },
            });
        }
        if (c.Has("reveal"))
        {
            var target = byId[c.Str("reveal")];
            tree.RevealItem(target, c.Str("strategy", "top") switch
            {
                "center" => ScrollStrategy.Center,
                "bottom" => ScrollStrategy.Bottom,
                _ => ScrollStrategy.Top,
            });
            tree.SelectedItem = target;
        }
        return tree;
    }
}
