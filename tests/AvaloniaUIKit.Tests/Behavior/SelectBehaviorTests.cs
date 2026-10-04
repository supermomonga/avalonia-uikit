using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:Select does beyond its look: GPUI Kit's Select and Combobox
/// rules for search, the cursor, keys, selection, will-change and clearing
/// (select.rs, combobox.rs, searchable_list/, list.rs, gpui-base select.rs).
/// </summary>
public class SelectBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    // Eight fruits, Banana selected, searchable (uikit-select's query cases).
    private static readonly GoldenCase Searchable = Case("uikit-select/query.base/click+wait-200ms+key-a+key-n+wait-200ms/light");

    private static (CaseHost Host, Select Select) Open(GoldenCase golden, bool combobox = false, Action<Select>? configure = null)
    {
        var select = Adapters.SelectCase(golden, combobox);
        configure?.Invoke(select);
        return (CaseHost.Open(golden, select), select);
    }

    private static List<SelectListItem> Rows(CaseHost host) =>
        host.Window.GetVisualDescendants().OfType<SelectListItem>().Where(r => r.IsEffectivelyVisible)
            .OrderBy(r => r.TranslatePoint(default, host.Window)!.Value.Y).ToList();

    private static string Shown(CaseHost host) => string.Join(", ", Rows(host).Select(r => r.Content));

    private static string? Cursor(CaseHost host) => Rows(host).FirstOrDefault(r => r.IsCursor)?.Content as string;

    private static string Checked(CaseHost host) => string.Join(", ", Rows(host).Where(r => r.IsSelected).Select(r => r.Content));

    private static string Selected(Select select) => string.Join(", ", select.SelectedItems!.Cast<object?>());

    private static void ClickRow(CaseHost host, GoldenCase golden, string text)
    {
        var row = Rows(host).Single(r => Equals(r.Content, text));
        var at = row.TranslatePoint(new Point(40, row.Bounds.Height / 2), host.Window)!.Value;
        host.Drive(golden, $"click-at-{at.X:0.##}-{at.Y:0.##}");
    }

    private static IInputElement? Focused(CaseHost host) => host.Window.FocusManager?.GetFocusedElement();

    // vec.rs perform_search: rows whose title contains the query, ignoring case;
    // list.rs start_search: the cursor goes to the first match, which titles the
    // Select's trigger (display_title reads the cursor).
    [Test]
    public async Task A_query_keeps_the_rows_that_contain_it_and_puts_the_cursor_on_the_first()
    {
        var (host, select) = Open(Searchable);
        using var _ = host;
        host.Drive(Searchable, "click+wait-200ms");
        await Assert.That(Focused(host)).IsSameReferenceAs(host.Part<TextBox>("PART_SearchBox"));
        host.Drive(Searchable, "key-A+key-n+wait-200ms");
        await Assert.That(select.SearchText).IsEqualTo("An");
        await Assert.That(Shown(host)).IsEqualTo("Banana, Mango, Orange");
        await Assert.That(Cursor(host)).IsEqualTo("Banana");
        await Assert.That(select.DisplayTitle).IsEqualTo("Banana");
        host.Drive(Searchable, "key-down");
        await Assert.That(select.DisplayTitle).IsEqualTo("Mango");
        // The committed value does not follow the cursor.
        await Assert.That(select.SelectedItem).IsEqualTo("Banana");
    }

    [Test]
    public async Task A_query_without_a_match_shows_the_empty_view_and_the_placeholder()
    {
        var (host, select) = Open(Searchable);
        using var _ = host;
        host.Drive(Searchable, "click+wait-200ms+key-x+key-z+wait-200ms");
        await Assert.That(Rows(host).Count).IsEqualTo(0);
        await Assert.That(host.Part<Panel>("PART_Empty").IsEffectivelyVisible).IsTrue();
        await Assert.That(select.DisplayTitle).IsNull();
    }

    // list.rs: the search field shows its spinner until 100ms after a search.
    [Test]
    public async Task The_search_field_spins_for_100ms_after_a_query()
    {
        var (host, select) = Open(Searchable);
        using var _ = host;
        host.Drive(Searchable, "click+wait-200ms+key-a");
        await Assert.That(host.Part<ProgressBar>("PART_SearchSpinner").IsEffectivelyVisible).IsTrue();
        await Assert.That(host.Part<Button>("PART_ClearSearchButton").IsEffectivelyVisible).IsFalse();
        host.Drive(Searchable, "wait-99ms");
        await Assert.That(host.Part<ProgressBar>("PART_SearchSpinner").IsEffectivelyVisible).IsTrue();
        host.Drive(Searchable, "wait-2ms");
        await Assert.That(host.Part<ProgressBar>("PART_SearchSpinner").IsEffectivelyVisible).IsFalse();
        await Assert.That(host.Part<Button>("PART_ClearSearchButton").IsEffectivelyVisible).IsTrue();
        // input.rs: the clear button empties the field and keeps its focus.
        var clear = host.Part<Button>("PART_ClearSearchButton");
        var at = clear.TranslatePoint(new Point(6, 6), host.Window)!.Value;
        host.Drive(Searchable, $"click-at-{at.X:0.##}-{at.Y:0.##}+wait-200ms");
        await Assert.That(select.SearchText).IsEqualTo(string.Empty);
        await Assert.That(Rows(host).Count).IsEqualTo(8);
        await Assert.That(Focused(host)).IsSameReferenceAs(host.Part<TextBox>("PART_SearchBox"));
    }

    // vec.rs SearchableGroup::matched_rows: a section stays with its matching
    // rows, or with none when its title matches; with no row anywhere, the
    // list shows its empty view (list.rs render_items).
    [Test]
    public async Task A_query_keeps_sections_by_their_rows_or_their_title()
    {
        var golden = Case("uikit-select/groupsearch.base/click+wait-200ms+key-e+wait-200ms/light");
        var (host, _) = Open(golden);
        using var _h = host;
        host.Drive(golden, "click+wait-200ms+key-e+wait-200ms");
        var headers = host.Window.GetVisualDescendants().OfType<SelectGroupHeader>().Where(h => h.IsEffectivelyVisible).Select(h => h.Content).ToList();
        await Assert.That(string.Join(", ", headers)).IsEqualTo("Fruits, Vegetables");
        await Assert.That(Shown(host)).IsEqualTo("Apple, Cherry, Leek");
        host.Drive(golden, "key-backspace+key-v+key-e+key-g+wait-200ms");
        await Assert.That(Rows(host).Count).IsEqualTo(0);
        await Assert.That(host.Part<Panel>("PART_Empty").IsEffectivelyVisible).IsTrue();
    }

    [Test]
    public async Task The_app_supplies_the_text_and_the_filter()
    {
        var (host, select) = Open(Searchable, configure: s =>
        {
            s.TextSelector = item => $"#{item}";
            // Prefix match instead of contains.
            s.Filter = (item, query) => ((string)item!).StartsWith(query, StringComparison.OrdinalIgnoreCase);
        });
        using var _ = host;
        await Assert.That(select.DisplayTitle).IsEqualTo("#Banana");
        host.Drive(Searchable, "click+wait-200ms+key-p+wait-200ms");
        await Assert.That(Shown(host)).IsEqualTo("#Peach");
    }

    // AsyncPopulator, as a delegate whose perform_search fetches: the rows come
    // when the task does; the spinner stays until 100ms after.
    [Test]
    public async Task An_async_populator_fetches_the_rows_for_a_query()
    {
        var pending = new TaskCompletionSource<IEnumerable<object>>();
        string? asked = null;
        var (host, _) = Open(Searchable, configure: s => s.AsyncPopulator = (query, _) =>
        {
            asked = query;
            return pending.Task;
        });
        using var _h = host;
        host.Drive(Searchable, "click+wait-200ms+key-q+wait-200ms");
        await Assert.That(asked).IsEqualTo("q");
        await Assert.That(host.Part<ProgressBar>("PART_SearchSpinner").IsEffectivelyVisible).IsTrue();
        pending.SetResult(["Quince", "Kumquat"]);
        host.Drive(Searchable, "wait-1ms");
        await Assert.That(Shown(host)).IsEqualTo("Quince, Kumquat");
        host.Drive(Searchable, "wait-101ms");
        await Assert.That(host.Part<ProgressBar>("PART_SearchSpinner").IsEffectivelyVisible).IsFalse();
    }

    // gpui-base select.rs: ↑ and ↓ open the closed select and leave the
    // cursor on the selection (they do not change it as ComboBox does).
    [Test]
    [Arguments("key-down")]
    [Arguments("key-up")]
    [Arguments("key-enter")]
    public async Task Keys_on_the_closed_trigger_open_it_without_changing_the_selection(string key)
    {
        var golden = Case("uikit-select/keys.base/focus+key-down+wait-200ms/light");
        var (host, select) = Open(golden);
        using var _ = host;
        host.Drive(golden, $"focus+{key}+wait-200ms");
        await Assert.That(select.IsDropDownOpen).IsTrue();
        await Assert.That(select.SelectedItem).IsEqualTo("Banana");
        await Assert.That(Cursor(host)).IsEqualTo("Banana");
    }

    // cache.rs: the cursor wraps at both ends; down from nothing goes to the
    // first row, up from nothing to the last.
    [Test]
    public async Task The_cursor_wraps_and_starts_from_either_end()
    {
        var golden = Case("uikit-select/openplaceholder.base/click+wait-200ms/light");
        var (host, select) = Open(golden);
        using var _ = host;
        host.Drive(golden, "click+wait-200ms");
        await Assert.That(Cursor(host)).IsNull();
        await Assert.That(select.DisplayTitle).IsNull();
        host.Drive(golden, "key-up");
        await Assert.That(Cursor(host)).IsEqualTo("Grape");
        host.Drive(golden, "key-down");
        await Assert.That(Cursor(host)).IsEqualTo("Apple");
        host.Drive(golden, "key-escape+click+wait-200ms+key-down");
        await Assert.That(Cursor(host)).IsEqualTo("Apple");
    }

    // Sections are not rows: the cursor steps over the headers.
    [Test]
    public async Task The_cursor_steps_over_section_headers()
    {
        var golden = Case("uikit-select/groups.base/click+wait-200ms/light");
        var (host, _) = Open(golden);
        using var _h = host;
        host.Drive(golden, "click+wait-200ms+key-down+key-down");
        await Assert.That(Cursor(host)).IsEqualTo("Carrot");
        host.Drive(golden, "key-up");
        await Assert.That(Cursor(host)).IsEqualTo("Cherry");
    }

    // select.rs on_confirm: Enter commits the cursor, closes, refocuses the
    // trigger and drops the query, so the next open shows every row.
    [Test]
    public async Task Enter_commits_the_cursor_and_the_next_open_shows_every_row()
    {
        var (host, select) = Open(Searchable);
        using var _ = host;
        var changes = new List<string>();
        select.SelectionChanged += (_, e) => changes.Add($"-{string.Join("|", e.RemovedItems.Cast<object>())} +{string.Join("|", e.AddedItems.Cast<object>())}");
        host.Drive(Searchable, "click+wait-200ms+key-a+key-n+wait-200ms+key-down+key-enter");
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.SelectedItem).IsEqualTo("Mango");
        await Assert.That(select.SelectedIndex).IsEqualTo(5);
        await Assert.That(select.SearchText).IsEqualTo(string.Empty);
        await Assert.That(Focused(host)).IsSameReferenceAs(select);
        await Assert.That(string.Join(";", changes)).IsEqualTo("-Banana +Mango");
        host.Drive(Searchable, "click+wait-200ms");
        await Assert.That(Rows(host).Count).IsEqualTo(8);
        await Assert.That(Cursor(host)).IsEqualTo("Mango");
    }

    // select.rs escape: Escape closes, keeps the selection and drops the query.
    [Test]
    public async Task Escape_closes_and_keeps_the_selection()
    {
        var (host, select) = Open(Searchable);
        using var _ = host;
        host.Drive(Searchable, "click+wait-200ms+key-a+key-n+wait-200ms+key-down+key-escape");
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.SelectedItem).IsEqualTo("Banana");
        await Assert.That(select.SearchText).IsEqualTo(string.Empty);
        await Assert.That(select.DisplayTitle).IsEqualTo("Banana");
        await Assert.That(Focused(host)).IsSameReferenceAs(select);
    }

    // select.rs on_blur: focus leaving the list closes it; Tab moves on from the trigger.
    [Test]
    public async Task Tab_closes_the_list_and_moves_on()
    {
        var select = Adapters.SelectCase(Searchable, combobox: false);
        var next = new TextBox { Width = 100 };
        var panel = new StackPanel { Spacing = 8, Children = { select, next } };
        using var host = CaseHost.Open(Searchable, panel);
        host.Drive(Searchable, "focus+key-down+wait-200ms+key-a");
        await Assert.That(select.IsDropDownOpen).IsTrue();
        host.PressKey("tab");
        host.Flush();
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.SearchText).IsEqualTo(string.Empty);
        await Assert.That(Focused(host)).IsSameReferenceAs(next);
    }

    // A click on the open trigger closes the list and keeps the trigger's focus (its ring).
    [Test]
    public async Task A_click_on_the_open_trigger_closes_it()
    {
        var golden = Case("uikit-select/closedafter.base/click+wait-200ms+key-escape/light");
        var (host, select) = Open(golden);
        using var _ = host;
        host.Drive(golden, "click+wait-200ms+click");
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(Focused(host)).IsSameReferenceAs(select);
        host.Drive(golden, "click+wait-200ms");
        await Assert.That(select.IsDropDownOpen).IsTrue();
    }

    [Test]
    public async Task Opening_and_closing_raise_events()
    {
        var golden = Case("uikit-select/closedafter.base/click+wait-200ms+key-escape/light");
        var (host, select) = Open(golden);
        using var _ = host;
        var events = new List<string>();
        select.DropDownOpened += (_, _) => events.Add("opened");
        select.DropDownClosed += (_, _) => events.Add("closed");
        host.Drive(golden, "click+wait-200ms+key-escape");
        await Assert.That(string.Join(",", events)).IsEqualTo("opened,closed");
    }

    // combobox.rs: a multiple selection toggles the clicked row, keeps the popup
    // open with the cursor on it, and titles the trigger with the titles joined.
    [Test]
    public async Task A_multiple_selection_toggles_rows_and_stays_open()
    {
        var golden = Case("uikit-combobox/multiple.base/click+wait-200ms/light");
        var (host, select) = Open(golden, combobox: true);
        using var _ = host;
        await Assert.That(select.DisplayTitle).IsEqualTo("Banana, Cherry");
        host.Drive(golden, "click+wait-200ms");
        ClickRow(host, golden, "Apple");
        await Assert.That(select.IsDropDownOpen).IsTrue();
        await Assert.That(Selected(select)).IsEqualTo("Banana, Cherry, Apple");
        await Assert.That(Cursor(host)).IsEqualTo("Apple");
        await Assert.That(Checked(host)).IsEqualTo("Apple, Banana, Cherry");
        ClickRow(host, golden, "Banana");
        await Assert.That(Selected(select)).IsEqualTo("Cherry, Apple");
        await Assert.That(select.DisplayTitle).IsEqualTo("Cherry, Apple");
        // Enter toggles the cursor's row as a click does.
        host.Drive(golden, "key-enter");
        await Assert.That(Selected(select)).IsEqualTo("Cherry, Apple, Banana");
        await Assert.That(select.IsDropDownOpen).IsTrue();
    }

    // on_will_change: a handler may cancel (the Combobox story's "Maximum
    // selections") or replace what the click proposes.
    [Test]
    public async Task SelectionChanging_cancels_or_replaces_the_proposed_selection()
    {
        var golden = Case("uikit-combobox/max.base/click+wait-200ms+click-at-60-120/light");
        var (host, select) = Open(golden, combobox: true);
        using var _ = host;
        var changing = new List<string>();
        select.SelectionChanging += (_, e) => changing.Add($"-{string.Join("|", e.RemovedItems)} +{string.Join("|", e.AddedItems)} = {string.Join("|", e.Selection)}");
        host.Drive(golden, "click+wait-200ms");
        ClickRow(host, golden, "Cherry");
        await Assert.That(Selected(select)).IsEqualTo("Apple, Banana");
        await Assert.That(Cursor(host)).IsEqualTo("Cherry");
        await Assert.That(string.Join(";", changing)).IsEqualTo("- +Cherry = Apple|Banana|Cherry");

        select.SelectionChanging += (_, e) =>
        {
            e.Cancel = false;
            e.Selection.Clear();
            e.Selection.Add("Grape");
        };
        ClickRow(host, golden, "Cherry");
        await Assert.That(Selected(select)).IsEqualTo("Grape");
    }

    // A single select closes on a click even when the handler cancels (select.rs on_confirm).
    [Test]
    public async Task A_cancelled_single_selection_still_closes()
    {
        var golden = Case("uikit-select/open.medium/click+wait-200ms/light");
        var (host, select) = Open(golden, configure: s => s.SelectionChanging += (_, e) => e.Cancel = true);
        using var _ = host;
        host.Drive(golden, "click+wait-200ms+click-at-60-64");
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.SelectedItem).IsEqualTo("Banana");
    }

    // A disabled row shows but cannot be chosen.
    [Test]
    public async Task A_disabled_row_cannot_be_chosen()
    {
        var golden = Case("uikit-select/rows.base/click+wait-200ms+at-60-156/light");
        var (host, select) = Open(golden);
        using var _ = host;
        host.Drive(golden, "click+wait-200ms+click-at-60-156");
        await Assert.That(select.IsDropDownOpen).IsTrue();
        await Assert.That(select.SelectedItem).IsEqualTo("Banana");
        // The cursor may stand on it (cache.rs does not skip disabled rows); Enter does nothing.
        host.Drive(golden, "key-down+key-down+key-enter");
        await Assert.That(Cursor(host)).IsEqualTo("Grape");
        await Assert.That(select.SelectedItem).IsEqualTo("Banana");
    }

    [Test]
    public async Task A_disabled_select_does_not_open()
    {
        var golden = Case("uikit-select/closed.medium/disabled/light");
        var (host, select) = Open(golden);
        using var _ = host;
        host.Drive(golden, "click+wait-200ms");
        await Assert.That(select.IsDropDownOpen).IsFalse();
        select.IsDropDownOpen = true;
        await Assert.That(select.IsDropDownOpen).IsFalse();
    }

    // select.rs clean / combobox.rs clear_selection: the clear button empties the
    // selection without opening the list, focusing the trigger or asking will-change.
    [Test]
    [Arguments("uikit-select/clear.base/normal/light", false)]
    [Arguments("uikit-combobox/clear.base/normal/light", true)]
    public async Task The_clear_button_clears_without_opening_or_focusing(string id, bool combobox)
    {
        var golden = Case(id);
        var (host, select) = Open(golden, combobox);
        using var _ = host;
        var changing = 0;
        var changed = 0;
        select.SelectionChanging += (_, _) => changing++;
        select.SelectionChanged += (_, _) => changed++;
        host.Drive(golden, "click-at-196-24");
        await Assert.That(select.SelectedItems!.Count).IsEqualTo(0);
        await Assert.That(select.SelectedItem).IsNull();
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.IsFocused).IsFalse();
        await Assert.That(changing).IsEqualTo(0);
        await Assert.That(changed).IsEqualTo(1);
        await Assert.That(host.Part<Button>("PART_ClearButton").IsEffectivelyVisible).IsFalse();
        await Assert.That(host.Part<PathIcon>("PART_Caret").IsEffectivelyVisible).IsTrue();
    }

    // The selection is the app's: SelectedItems, SelectedItem and SelectedIndex
    // follow each other, and set them without will-change.
    [Test]
    public async Task The_selection_properties_follow_each_other()
    {
        var golden = Case("uikit-combobox/multiple.base/normal/light");
        var (host, select) = Open(golden, combobox: true);
        using var _ = host;
        var list = new System.Collections.ObjectModel.ObservableCollection<object?> { "Grape" };
        select.SelectedItems = list;
        await Assert.That(select.SelectedItem).IsEqualTo("Grape");
        await Assert.That(select.SelectedIndex).IsEqualTo(3);
        list.Add("Apple");
        await Assert.That(select.DisplayTitle).IsEqualTo("Grape, Apple");
        select.SelectedIndex = 1;
        await Assert.That(string.Join(", ", list)).IsEqualTo("Banana");
        select.SelectedItem = null;
        await Assert.That(list.Count).IsEqualTo(0);
        await Assert.That(select.SelectedIndex).IsEqualTo(-1);
        await Assert.That(select.DisplayTitle).IsNull();
    }

    // An index set before the items (XAML attributes come before content) applies when they arrive.
    [Test]
    public async Task An_index_set_before_the_items_applies_when_they_arrive()
    {
        var select = new Select { SelectedIndex = 2 };
        select.Items.Add("Apple");
        select.Items.Add("Banana");
        await Assert.That(select.SelectedItem).IsNull();
        select.Items.Add("Cherry");
        await Assert.That(select.SelectedItem).IsEqualTo("Cherry");
        await Assert.That(select.SelectedIndex).IsEqualTo(2);
    }

    // Invalid (DataValidationErrors), the frame takes the danger border, as ComboBox's does.
    [Test]
    public async Task A_validation_error_shows_the_danger_border()
    {
        var golden = Case("uikit-select/closed.medium/normal/light");
        var (host, select) = Open(golden);
        using var _ = host;
        DataValidationErrors.SetError(select, new InvalidOperationException("Required"));
        host.Flush();
        var frame = host.Part<Border>("PART_Background");
        var danger = (Avalonia.Media.ISolidColorBrush)Avalonia.Application.Current!.FindResource(golden.Variant, "UIKit.Danger")!;
        await Assert.That(((Avalonia.Media.ISolidColorBrush)frame.BorderBrush!).Color).IsEqualTo(danger.Color);
    }

    // The rows are virtualized: a long list creates only the rows it shows.
    [Test]
    public async Task A_long_list_creates_only_the_rows_it_shows()
    {
        var golden = Case("uikit-select/scroll.base/click+wait-200ms/light");
        var (host, select) = Open(golden, configure: s =>
        {
            s.Items.Clear();
            for (var i = 0; i < 1000; i++)
            {
                s.Items.Add($"Item {i + 1}");
            }
            s.SelectedIndex = 500;
        });
        using var _ = host;
        host.Drive(golden, "click+wait-200ms");
        var rows = host.Window.GetVisualDescendants().OfType<SelectListItem>().Count();
        await Assert.That(rows).IsLessThan(40);
        await Assert.That(Cursor(host)).IsEqualTo("Item 501");
        host.Drive(golden, "key-down");
        await Assert.That(Cursor(host)).IsEqualTo("Item 502");
    }
}
