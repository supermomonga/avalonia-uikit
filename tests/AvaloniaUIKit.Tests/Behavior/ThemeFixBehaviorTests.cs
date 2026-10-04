using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// The features the standard controls already have that the theme now carries:
/// they still work through the theme's parts, with the GPUI Kit rules the look
/// follows.
/// </summary>
public class ThemeFixBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private static T Part<T>(Visual root, string name) where T : Visual =>
        root.GetVisualDescendants().OfType<T>().First(v => (v as StyledElement)?.Name == name);

    [Test]
    public async Task A_drop_down_button_opens_its_flyout_and_shows_as_selected()
    {
        var golden = Case("dropdownbutton/open.base/click/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var button = (DropDownButton)host.Control;
        await Assert.That(Part<PathIcon>(button, "PART_Caret").IsEffectivelyVisible).IsTrue();
        host.Drive(golden, "click");
        await Assert.That(button.Flyout!.IsOpen).IsTrue();
        await Assert.That(button.Classes.Contains(":flyout-open")).IsTrue();
    }

    [Test]
    public async Task A_number_input_keeps_stepping_with_a_prefix_and_suffix()
    {
        var golden = Case("number/icons.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var number = (NumericUpDown)host.Control;
        var text = Part<TextBox>(number, "PART_TextBox");
        // GPUI's order: prefix, the value, suffix, between the step buttons.
        var prefix = Part<ContentPresenter>(text, "PART_InnerLeftContent");
        var suffix = Part<ContentPresenter>(text, "PART_InnerRightContent");
        var value = Part<TextPresenter>(text, "PART_TextPresenter");
        await Assert.That(prefix.TranslatePoint(default, number)!.Value.X).IsLessThan(value.TranslatePoint(default, number)!.Value.X);
        await Assert.That(suffix.TranslatePoint(default, number)!.Value.X).IsGreaterThan(value.TranslatePoint(default, number)!.Value.X);
        host.Drive(golden, "click-at-160-24");
        await Assert.That(number.Value).IsEqualTo(43m);
    }

    // delegate.rs render_empty: the empty view shows while the table has no rows.
    [Test]
    public async Task A_data_table_shows_its_empty_view_only_without_rows()
    {
        var golden = Case("datatable/empty.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var table = (TableView)host.Control;
        var empty = Part<Panel>(table, "PART_Empty");
        await Assert.That(empty.IsEffectivelyVisible).IsTrue();
        var rows = new System.Collections.ObjectModel.ObservableCollection<Adapters.Person>();
        table.ItemsSource = rows;
        rows.Add(Adapters.People[0]);
        host.Flush();
        await Assert.That(empty.IsEffectivelyVisible).IsFalse();
        rows.Clear();
        host.Flush();
        await Assert.That(empty.IsEffectivelyVisible).IsTrue();
    }

    [Test]
    public async Task A_table_shows_no_empty_view()
    {
        var golden = Case("table/empty.bordered/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        await Assert.That(host.Control.GetVisualDescendants().OfType<Panel>().Any(p => p.Name == "PART_Empty")).IsFalse();
    }

    [Test]
    public async Task A_data_grid_shows_its_empty_view_only_without_rows()
    {
        var golden = Case("datagrid/empty.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var grid = (DataGrid)host.Control;
        var empty = Part<Panel>(grid, "PART_Empty");
        await Assert.That(empty.IsEffectivelyVisible).IsTrue();
        grid.ItemsSource = Adapters.People;
        host.Flush();
        await Assert.That(empty.IsEffectivelyVisible).IsFalse();
    }

    // state.rs TableSelection::Cell: the selected cell follows the keyboard in its
    // row and column; the row itself does not show as selected.
    [Test]
    public async Task A_cell_selectable_data_grid_marks_the_current_cell_of_the_selected_row()
    {
        var golden = Case("datagrid/cell.base/click-at-200-97/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var grid = (DataGrid)host.Control;
        DataGridCell[] Marked() => grid.GetVisualDescendants().OfType<DataGridCell>()
            .Where(c => Part<Border>(c, "PART_SelectedCell").IsEffectivelyVisible).ToArray();
        await Assert.That(Marked()).IsEmpty();
        host.Drive(golden, "click-at-200-97+at-200-270");
        var marked = Marked();
        await Assert.That(marked.Length).IsEqualTo(1);
        await Assert.That(((Adapters.Person)marked[0].DataContext!).Name).IsEqualTo("Grace");
        await Assert.That(marked[0].Bounds.X).IsEqualTo(120);
        var row = marked[0].FindAncestorOfType<DataGridRow>()!;
        await Assert.That(Part<Border>(row, "BackgroundRectangle").Background).IsNull();
        host.PressKey("right");
        host.Flush();
        marked = Marked();
        await Assert.That(marked.Length).IsEqualTo(1);
        await Assert.That(marked[0].Bounds.X).IsEqualTo(236);
        host.PressKey("down");
        host.Flush();
        marked = Marked();
        await Assert.That(marked.Length).IsEqualTo(1);
        await Assert.That(((Adapters.Person)marked[0].DataContext!).Name).IsEqualTo("Linus");
    }
}
