using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:ListView and uikit:Tree do beyond their look: the GPUI Kit rules
/// of list.rs, cache.rs and base/tree.rs (selection, confirm and cancel, the
/// keys, the right-clicked mark, search, loading more, scrolling to a row) and
/// the virtualization of their rows.
/// </summary>
public class ListsBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    /// <summary>The realized rows, top to bottom.</summary>
    private static List<ListItem> Rows(Control control) =>
        control.GetVisualDescendants().OfType<ListItem>()
            .Where(r => r.IsEffectivelyVisible && r.FindAncestorOfType<ListRows>() is not null)
            .OrderBy(r => r.TranslatePoint(default, control)!.Value.Y)
            .ToList();

    private static ListItem Row(Control control, string text) =>
        Rows(control).First(r => (r.Content is TreeEntry entry ? entry.Item : r.Content)?.ToString() == text);

    private static Point Center(CaseHost host, Visual visual) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 3, visual.Bounds.Height / 2), host.Window)!.Value;

    private static void Click(CaseHost host, Visual visual, MouseButton button = MouseButton.Left, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var at = Center(host, visual);
        host.Window.MouseMove(at, modifiers);
        host.Window.MouseDown(at, button, modifiers);
        host.Window.MouseUp(at, button, modifiers);
        host.Flush();
    }

    private static void Key(CaseHost host, string key)
    {
        host.PressKey(key);
        host.Flush();
    }

    /// <summary>GPUI's "secondary" modifier, as the list reads it.</summary>
    private static (RawInputModifiers Raw, string Prefix) Secondary() =>
        Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers == KeyModifiers.Meta
            ? (RawInputModifiers.Meta, "cmd")
            : (RawInputModifiers.Control, "ctrl");

    private static (CaseHost Host, ListView List) OpenList(string id = "uikit-list/unselected.base/normal/light")
    {
        var golden = Case(id);
        var host = CaseHost.Open(golden, Adapters.Create(golden));
        return (host, (ListView)host.Control);
    }

    // list.rs: a click selects the row and confirms it; with the secondary
    // modifier the confirm is secondary.
    [Test]
    public async Task A_click_on_a_row_selects_and_confirms_it()
    {
        var (host, list) = OpenList();
        using var _ = host;
        var confirmed = new List<(object? Item, bool Secondary)>();
        list.Confirmed += (_, e) => confirmed.Add((e.Item, e.IsSecondary));
        Click(host, Row(list, "Cherry"));
        await Assert.That(list.SelectedIndex).IsEqualTo(new IndexPath(0, 2));
        await Assert.That(list.SelectedItem).IsEqualTo("Cherry");
        Click(host, Row(list, "Lemon"), modifiers: Secondary().Raw);
        await Assert.That(string.Join(",", confirmed.Select(c => $"{c.Item}:{c.Secondary}"))).IsEqualTo("Cherry:False,Lemon:True");
        await Assert.That(Row(list, "Lemon").IsSelected).IsTrue();
        await Assert.That(Row(list, "Cherry").IsSelected).IsFalse();
    }

    // list.rs: Enter confirms the selected row (secondary-enter as secondary);
    // Esc clears the selection and emits Cancel.
    [Test]
    public async Task Enter_confirms_and_escape_cancels()
    {
        var (host, list) = OpenList("uikit-list/rows.base/normal/light");
        using var _ = host;
        var confirmed = new List<string>();
        var cancelled = 0;
        list.Confirmed += (_, e) => confirmed.Add($"{e.Item}:{e.IsSecondary}");
        list.Cancelled += (_, _) => cancelled++;
        list.Focus();
        Key(host, "enter");
        Key(host, Secondary().Prefix + "-enter");
        await Assert.That(string.Join(",", confirmed)).IsEqualTo("Banana:False,Banana:True");
        Key(host, "escape");
        await Assert.That(list.SelectedIndex).IsNull();
        await Assert.That(cancelled).IsEqualTo(1);
        // Without a selection there is nothing to confirm.
        Key(host, "enter");
        await Assert.That(confirmed.Count).IsEqualTo(2);
    }

    // cache.rs: Down from no selection selects the first row, Up the last; both
    // wrap at the ends and skip the headers, the footers and empty sections.
    [Test]
    public async Task The_keys_wrap_and_skip_headers_and_empty_sections()
    {
        var (host, list) = OpenList("uikit-list/sections.base/normal/light");
        using var _ = host;
        list.Focus();
        Key(host, "down");
        await Assert.That(list.SelectedIndex).IsEqualTo(new IndexPath(0, 0));
        Key(host, "up");
        await Assert.That(list.SelectedIndex).IsEqualTo(new IndexPath(2, 2));
        Key(host, "down");
        await Assert.That(list.SelectedIndex).IsEqualTo(new IndexPath(0, 0));
        Key(host, "down");
        Key(host, "down");
        Key(host, "down");
        await Assert.That(list.SelectedIndex).IsEqualTo(new IndexPath(2, 0));
        await Assert.That(list.SelectedItem).IsEqualTo("Lemon");
        // The row came into view, at the bottom edge.
        var lemon = Row(list, "Lemon");
        var bottom = lemon.TranslatePoint(new Point(0, lemon.Bounds.Height), list)!.Value.Y;
        await Assert.That(Math.Abs(bottom - list.Bounds.Height)).IsLessThan(0.01);
    }

    // list.rs: a right press marks the row (set_right_clicked_index) without
    // selecting it; a click on a row and a press outside the list clear the mark.
    [Test]
    public async Task A_right_press_marks_a_row_without_selecting_it()
    {
        var (host, list) = OpenList("uikit-list/rows.base/normal/light");
        using var _ = host;
        Click(host, Row(list, "Cherry"), MouseButton.Right);
        await Assert.That(list.RightClickedIndex).IsEqualTo(new IndexPath(0, 2));
        await Assert.That(list.SelectedItem).IsEqualTo("Banana");
        await Assert.That(Row(list, "Cherry").IsSecondarySelected).IsTrue();
        Click(host, Row(list, "Lemon"));
        await Assert.That(list.RightClickedIndex).IsNull();
        await Assert.That(list.SelectedItem).IsEqualTo("Lemon");

        Click(host, Row(list, "Apple"), MouseButton.Right);
        var outside = new Point(list.Bounds.Width + 30, 20);
        host.Window.MouseDown(outside, MouseButton.Left);
        host.Window.MouseUp(outside, MouseButton.Left);
        host.Flush();
        await Assert.That(list.RightClickedIndex).IsNull();
        await Assert.That(Row(list, "Apple").IsSecondarySelected).IsFalse();
    }

    // list.rs selectable(false): no selection or confirm from clicks or keys, no mark.
    [Test]
    public async Task An_unselectable_list_selects_nothing()
    {
        var (host, list) = OpenList("uikit-list/unselectable.base/click-at-60-59/light");
        using var _ = host;
        var confirmed = 0;
        list.Confirmed += (_, _) => confirmed++;
        Click(host, Row(list, "Banana"));
        Click(host, Row(list, "Cherry"), MouseButton.Right);
        Key(host, "down");
        Key(host, "enter");
        await Assert.That(list.SelectedIndex).IsNull();
        await Assert.That(list.RightClickedIndex).IsNull();
        await Assert.That(confirmed).IsEqualTo(0);
    }

    // list.rs start_search: the query filters the rows through the delegate and
    // selects the first; no match leaves the empty view; the keys work while
    // the search field has the focus.
    [Test]
    public async Task The_search_field_filters_the_rows_and_selects_the_first()
    {
        var (host, list) = OpenList("uikit-list/search.base/normal/light");
        using var _ = host;
        var box = host.Part<TextBox>("PART_SearchBox");
        box.Focus();
        host.Window.KeyTextInput("an");
        host.Flush();
        await Assert.That(list.SearchText).IsEqualTo("an");
        await Assert.That(string.Join(",", Rows(list).Select(r => r.Content?.ToString()))).IsEqualTo("Banana,Mango,Orange");
        await Assert.That(list.SelectedItem).IsEqualTo("Banana");
        Key(host, "down");
        await Assert.That(list.SelectedItem).IsEqualTo("Mango");
        await Assert.That(box.IsFocused).IsTrue();

        list.SearchFilter = (item, query) => item?.ToString()?.StartsWith(query, StringComparison.OrdinalIgnoreCase) == true;
        list.SearchText = "p";
        host.Flush();
        await Assert.That(string.Join(",", Rows(list).Select(r => r.Content?.ToString()))).IsEqualTo("Peach,Pear,Plum");

        list.SearchText = "xyz";
        host.Flush();
        await Assert.That(list.ItemCount).IsEqualTo(0);
        await Assert.That(host.Part<Panel>("PART_EmptyView").IsEffectivelyVisible).IsTrue();
    }

    // list.rs binds its keys only while it is not loading; the skeleton replaces the rows.
    [Test]
    public async Task A_loading_list_shows_the_skeleton_and_ignores_the_keys()
    {
        var (host, list) = OpenList("uikit-list/loading.base/normal/light");
        using var _ = host;
        list.Focus();
        Key(host, "down");
        await Assert.That(list.SelectedIndex).IsNull();
        await Assert.That(host.Part<StackPanel>("PART_Loading").IsEffectivelyVisible).IsTrue();
        await Assert.That(host.Part<ListRows>("PART_Rows").IsEffectivelyVisible).IsFalse();
        list.IsLoading = false;
        host.Flush();
        Key(host, "down");
        await Assert.That(list.SelectedIndex).IsEqualTo(new IndexPath(0, 0));
    }

    // list.rs load_more_if_need: near the end (threshold rows), with has_more,
    // the list asks for more once; IsLoadingMore holds it until the app is done.
    [Test]
    public async Task Scrolling_near_the_end_asks_for_more()
    {
        var golden = Case("uikit-list/unselected.base/normal/light");
        var items = new ObservableCollection<string>(Enumerable.Range(1, 100).Select(i => $"Row {i}"));
        var list = new ListView { Width = 240, Height = 200, ItemsSource = items, HasMore = true, LoadMoreThreshold = 20 };
        using var host = CaseHost.Open(golden, list);
        var requests = 0;
        list.LoadMore += (_, _) => requests++;
        host.Flush();
        await Assert.That(requests).IsEqualTo(0);

        list.ScrollToItem(new IndexPath(0, 85), ScrollStrategy.Center);
        host.Flush();
        Dispatcher.UIThread.RunJobs();
        await Assert.That(requests).IsEqualTo(1);
        await Assert.That(list.IsLoadingMore).IsTrue();
        list.ScrollToItem(new IndexPath(0, 90), ScrollStrategy.Center);
        host.Flush();
        Dispatcher.UIThread.RunJobs();
        await Assert.That(requests).IsEqualTo(1);

        for (var i = 101; i <= 150; i++)
        {
            items.Add($"Row {i}");
        }
        list.IsLoadingMore = false;
        host.Flush();
        Dispatcher.UIThread.RunJobs();
        await Assert.That(requests).IsEqualTo(1);
        list.ScrollToItem(new IndexPath(0, 140), ScrollStrategy.Center);
        host.Flush();
        Dispatcher.UIThread.RunJobs();
        await Assert.That(requests).IsEqualTo(2);
    }

    // scroll_to_item: Center centers the row; Top (the virtual list's nearest
    // rule) moves a row out of view to the nearer edge and leaves one in view.
    [Test]
    public async Task Scrolling_to_a_row_follows_the_strategy()
    {
        var golden = Case("uikit-list/unselected.base/normal/light");
        var list = new ListView { Width = 240, Height = 200, ItemsSource = Enumerable.Range(0, 1000).Select(i => $"Row {i}").ToList() };
        using var host = CaseHost.Open(golden, list);
        double Top(string text) => Row(list, text).TranslatePoint(default, list)!.Value.Y;
        list.ScrollToItem(new IndexPath(0, 500), ScrollStrategy.Center);
        host.Flush();
        await Assert.That(Math.Abs(Top("Row 500") + 17 - 100)).IsLessThan(0.01);
        list.ScrollToItem(new IndexPath(0, 502), ScrollStrategy.Top);
        host.Flush();
        await Assert.That(Math.Abs(Top("Row 500") + 17 - 100)).IsLessThan(0.01);
        list.ScrollToItem(new IndexPath(0, 900), ScrollStrategy.Top);
        host.Flush();
        await Assert.That(Math.Abs(Top("Row 900") + 34 - 200)).IsLessThan(0.01);
        list.ScrollToItem(new IndexPath(0, 0));
        host.Flush();
        await Assert.That(Top("Row 0")).IsEqualTo(0);
    }

    // GPUI's List paints the visible rows only.
    [Test]
    public async Task A_long_list_realizes_only_the_visible_rows()
    {
        var golden = Case("uikit-list/unselected.base/normal/light");
        var list = new ListView { Width = 240, Height = 200, ItemsSource = Enumerable.Range(0, 10000).Select(i => $"Row {i}").ToList() };
        using var host = CaseHost.Open(golden, list);
        var rows = host.Part<ListRows>("PART_Rows");
        await Assert.That(rows.GetRealizedContainers().Count()).IsLessThanOrEqualTo(7);
        list.ScrollToItem(new IndexPath(0, 9000), ScrollStrategy.Center);
        host.Flush();
        await Assert.That(rows.GetRealizedContainers().Count()).IsLessThanOrEqualTo(8);
        await Assert.That(Rows(list).Any(r => (string?)r.Content == "Row 9000")).IsTrue();
    }

    // XAML sets SelectedItem before the items: the list selects it once they come.
    [Test]
    public async Task A_selected_item_set_before_the_items_is_selected()
    {
        var golden = Case("uikit-list/unselected.base/normal/light");
        var list = new ListView { Width = 240, Height = 200, SelectedItem = "Banana" };
        list.Items.Add(new ListSection { Header = "Fruits", Items = { "Apple", "Banana" } });
        using var host = CaseHost.Open(golden, list);
        await Assert.That(list.SelectedIndex).IsEqualTo(new IndexPath(0, 1));
        await Assert.That(Row(list, "Banana").IsSelected).IsTrue();
    }

    private static (CaseHost Host, Tree Tree, Dictionary<string, TreeItem> ById) OpenTree(string kind = "sample", int extra = 0)
    {
        var golden = Case("uikit-tree/rows.base/normal/light");
        var (roots, byId) = Adapters.TreeItems(kind, extra);
        var tree = new Tree { Width = 240, Height = 238, ItemsSource = roots };
        return (CaseHost.Open(golden, tree), tree, byId);
    }

    // base/tree.rs on_entry_click: a press selects the row and toggles a folder
    // (Expanded / Collapsed); a file only gets selected; a disabled row ignores it.
    [Test]
    public async Task A_press_selects_a_row_and_toggles_a_folder()
    {
        var (host, tree, byId) = OpenTree();
        using var _ = host;
        var events = new List<string>();
        tree.Expanded += (_, e) => events.Add($"+{e.Item}");
        tree.Collapsed += (_, e) => events.Add($"-{e.Item}");
        tree.SelectionChanged += (_, e) => events.Add($"={e.Item}");
        Click(host, Row(tree, "components"));
        await Assert.That(tree.SelectedItem).IsEqualTo(byId["src/components"]);
        await Assert.That(byId["src/components"].IsExpanded).IsFalse();
        await Assert.That(tree.Entries.Count).IsEqualTo(5);
        Click(host, Row(tree, "lib.rs"));
        Click(host, Row(tree, "components"));
        await Assert.That(tree.Entries.Count).IsEqualTo(7);
        await Assert.That(string.Join(",", events)).IsEqualTo("=components,-components,=lib.rs,=components,+components");
        Click(host, Row(tree, "assets"));
        await Assert.That(tree.SelectedItem).IsEqualTo(byId["src/components"]);
        await Assert.That(byId["assets"].IsExpanded).IsFalse();
    }

    // base/tree.rs: Up and Down wrap (Down from no selection goes to row 1, as
    // GPUI's unwrap_or(0) + 1); Right expands and Left collapses the selected
    // folder; Enter (the Confirm action) toggles it.
    [Test]
    public async Task The_keys_move_and_toggle_as_gpui()
    {
        var (host, tree, byId) = OpenTree();
        using var _ = host;
        tree.Focus();
        Key(host, "down");
        await Assert.That(tree.SelectedIndex).IsEqualTo(1);
        Key(host, "left");
        await Assert.That(byId["src/components"].IsExpanded).IsFalse();
        Key(host, "left");
        await Assert.That(byId["src/components"].IsExpanded).IsFalse();
        Key(host, "right");
        await Assert.That(byId["src/components"].IsExpanded).IsTrue();
        Key(host, "enter");
        await Assert.That(byId["src/components"].IsExpanded).IsFalse();
        Key(host, "up");
        Key(host, "up");
        await Assert.That(tree.SelectedItem).IsEqualTo(byId["Cargo.toml"]);
        Key(host, "down");
        await Assert.That(tree.SelectedItem).IsEqualTo(byId["src"]);
        // A file: Left, Right and Enter do nothing.
        tree.SelectedItem = byId["src/lib.rs"];
        var rows = tree.Entries.Count;
        Key(host, "right");
        Key(host, "enter");
        await Assert.That(tree.Entries.Count).IsEqualTo(rows);
    }

    // base/tree.rs: a right press marks a row without selecting it; toggling a
    // folder clears the mark, selecting a file keeps it.
    [Test]
    public async Task A_right_press_marks_a_tree_row_without_selecting_it()
    {
        var (host, tree, byId) = OpenTree();
        using var _ = host;
        Click(host, Row(tree, "button.rs"), MouseButton.Right);
        await Assert.That(tree.RightClickedItem).IsEqualTo(byId["src/components/button.rs"]);
        await Assert.That(tree.SelectedItem).IsNull();
        await Assert.That(Row(tree, "button.rs").IsSecondarySelected).IsTrue();
        Click(host, Row(tree, "tree.rs"));
        await Assert.That(tree.RightClickedItem).IsEqualTo(byId["src/components/button.rs"]);
        Click(host, Row(tree, "src"));
        await Assert.That(tree.RightClickedItem).IsNull();
    }

    // base/tree.rs reveal_item / set_selected_item: the ancestors open and the
    // item scrolls into view.
    [Test]
    public async Task Revealing_an_item_expands_its_ancestors_and_scrolls_to_it()
    {
        var (host, tree, byId) = OpenTree("deep", extra: 40);
        using var _ = host;
        var expanded = new List<string>();
        tree.Expanded += (_, e) => expanded.Add(e.Item!.ToString()!);
        var target = byId["src/ui/widgets/widget_30.rs"];
        tree.RevealItem(target, ScrollStrategy.Center);
        host.Flush();
        await Assert.That(string.Join(",", expanded)).IsEqualTo("ui,widgets");
        var row = Row(tree, "widget_30.rs");
        var center = row.TranslatePoint(new Point(0, row.Bounds.Height / 2), tree)!.Value.Y;
        await Assert.That(Math.Abs(center - tree.Bounds.Height / 2)).IsLessThan(0.01);

        tree.SelectedItem = byId["docs/guide.md"];
        host.Flush();
        await Assert.That(byId["docs"].IsExpanded).IsTrue();
        await Assert.That(tree.SelectedIndex).IsEqualTo(tree.IndexOf(byId["docs/guide.md"]));
    }

    // GPUI's Tree flattens the visible items into a uniform list and paints
    // the visible rows only.
    [Test]
    public async Task A_big_tree_realizes_only_the_visible_rows()
    {
        var golden = Case("uikit-tree/rows.base/normal/light");
        var roots = Enumerable.Range(0, 100).Select(i => new TreeItem
        {
            Label = $"folder {i}",
            IsExpanded = true,
        }).ToList();
        foreach (var root in roots)
        {
            for (var j = 0; j < 100; j++)
            {
                root.Children.Add(new TreeItem { Label = $"{root.Label}/file {j}" });
            }
        }
        var tree = new Tree { Width = 240, Height = 238, ItemsSource = roots };
        using var host = CaseHost.Open(golden, tree);
        await Assert.That(tree.Entries.Count).IsEqualTo(10100);
        var rows = host.Part<ListRows>("PART_Rows");
        await Assert.That(rows.GetRealizedContainers().Count()).IsLessThanOrEqualTo(8);
        tree.RevealItem(roots[80].Children[50], ScrollStrategy.Top);
        host.Flush();
        await Assert.That(rows.GetRealizedContainers().Count()).IsLessThanOrEqualTo(8);
        await Assert.That(Rows(tree)[0].Content is TreeEntry { Item: TreeItem { Label: "folder 80/file 50" } }).IsTrue();
    }

    // Any data: children from a selector or a tree data template, the
    // expansion tracked by the tree, and disabled items from a selector.
    [Test]
    public async Task A_tree_shows_app_data_through_its_selectors()
    {
        var golden = Case("uikit-tree/rows.base/normal/light");
        var data = new Folder("root", [new Folder("a", [new Folder("a1", []), new Folder("a2", [])]), new Folder("b", [])]);
        var tree = new Tree
        {
            Width = 240,
            Height = 238,
            ItemsSource = new[] { data },
            ItemTemplate = new FuncTreeDataTemplate<Folder>((f, _) => new TextBlock { Text = f.Name }, f => f.Children),
            DisabledSelector = item => ((Folder)item).Name == "b",
        };
        using var host = CaseHost.Open(golden, tree);
        await Assert.That(tree.Entries.Count).IsEqualTo(1);
        tree.Expand(data);
        host.Flush();
        await Assert.That(string.Join(",", tree.Entries.Select(e => ((Folder)e.Item).Name))).IsEqualTo("root,a,b");
        await Assert.That(tree.Entries[2].IsDisabled).IsTrue();
        await Assert.That(tree.Entries[2].IsFolder).IsFalse();
        Click(host, Row(tree, "a"));
        await Assert.That(tree.IsItemExpanded(data.Children[0])).IsTrue();
        await Assert.That(string.Join(",", tree.Entries.Select(e => e.Depth))).IsEqualTo("0,1,2,2,1");
        await Assert.That(Rows(tree).Last().FindDescendantOfType<TextBlock>()?.Text).IsEqualTo("b");

        tree.ChildrenSelector = item => ((Folder)item).Name == "root" ? (IEnumerable)new[] { data.Children[1] } : null;
        host.Flush();
        await Assert.That(string.Join(",", tree.Entries.Select(e => ((Folder)e.Item).Name))).IsEqualTo("root,b");
    }

    private sealed record Folder(string Name, Folder[] Children)
    {
        public override string ToString() => Name;
    }
}
