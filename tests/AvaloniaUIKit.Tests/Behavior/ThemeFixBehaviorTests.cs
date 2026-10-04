using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
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

    // An editable ComboBox (Avalonia's rules): Tab lands in the field, typing an
    // item's text selects it, F4 and Alt+Down open the list, a selection writes
    // its text into the field.
    [Test]
    public async Task An_editable_combo_box_types_into_its_field()
    {
        var golden = Case("select/openplaceholder.base/click+wait-200ms/light");
        using var host = CaseHost.Open(golden, Adapters.EditableSelect(golden));
        var box = (ComboBox)host.Control;
        var field = Part<TextBox>(box, "PART_EditableTextBox");
        await Assert.That(field.IsEffectivelyVisible).IsTrue();
        host.Drive(golden, "focus");
        await Assert.That(field.IsFocused).IsTrue();
        await Assert.That(Part<Border>(box, "PART_FocusRing").IsVisible).IsTrue();
        host.Window.KeyTextInput("cherry");
        host.Flush();
        await Assert.That(box.SelectedIndex).IsEqualTo(2);
        await Assert.That(field.Text).IsEqualTo("cherry");
        box.SelectedIndex = 3;
        host.Flush();
        await Assert.That(field.Text).IsEqualTo("Grape");
    }

    [Test]
    [Arguments("f4")]
    [Arguments("alt-down")]
    public async Task An_editable_combo_box_opens_from_the_keyboard(string key)
    {
        var golden = Case("select/openplaceholder.base/click+wait-200ms/light");
        using var host = CaseHost.Open(golden, Adapters.EditableSelect(golden));
        var box = (ComboBox)host.Control;
        host.Drive(golden, "focus");
        // Enter and Space edit the text instead (ComboBox.OnKeyDown).
        host.PressKey("enter");
        host.Flush();
        await Assert.That(box.IsDropDownOpen).IsFalse();
        if (key == "f4")
        {
            host.Window.KeyPressQwerty(PhysicalKey.F4, RawInputModifiers.None);
            host.Window.KeyReleaseQwerty(PhysicalKey.F4, RawInputModifiers.None);
        }
        else
        {
            host.PressKey(key);
        }
        host.Flush();
        await Assert.That(box.IsDropDownOpen).IsTrue();
    }

    // input.rs cleanable: the clear button shows while the single-line text is
    // editable and not empty; a click clears the text and focuses the input.
    [Test]
    public async Task A_clear_button_clears_the_text_and_focuses_the_input()
    {
        var golden = Case("input/cleanable-pointer.base/click-at-199-28/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var box = (TextBox)host.Control;
        var clear = Part<Button>(box, "PART_ClearButton");
        await Assert.That(clear.IsEffectivelyVisible).IsTrue();
        host.Drive(golden, "click-at-199-28");
        await Assert.That(box.Text).IsEqualTo("");
        await Assert.That(box.IsFocused).IsTrue();
        await Assert.That(clear.IsEffectivelyVisible).IsFalse();
        host.Window.KeyTextInput("Hi");
        host.Flush();
        await Assert.That(clear.IsEffectivelyVisible).IsTrue();
        box.IsReadOnly = true;
        host.Flush();
        await Assert.That(clear.IsEffectivelyVisible).IsFalse();
        box.IsReadOnly = false;
        box.IsEnabled = false;
        host.Flush();
        await Assert.That(clear.IsEffectivelyVisible).IsFalse();
    }

    [Test]
    public async Task A_multi_line_text_box_shows_no_clear_button()
    {
        var box = new TextBox { Classes = { "clearButton" }, AcceptsReturn = true, Text = "Hello", Width = 200 };
        var golden = Case("input/cleanable.medium/normal/light");
        using var host = CaseHost.Open(golden, box);
        await Assert.That(Part<Button>(box, "PART_ClearButton").IsEffectivelyVisible).IsFalse();
    }

    // GPUI's suffix row: mask toggle, clear button, the app's suffix, a gap apart.
    [Test]
    public async Task The_suffix_row_keeps_gpui_order()
    {
        var golden = Case("input/cleanable-suffix.base/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var box = (TextBox)host.Control;
        double X(Visual v) => v.TranslatePoint(default, box)!.Value.X;
        var reveal = Part<ToggleButton>(box, "PART_RevealButton");
        var clear = Part<Button>(box, "PART_ClearButton");
        var suffix = Part<ContentPresenter>(box, "PART_InnerRightContent");
        await Assert.That(X(reveal)).IsLessThan(X(clear));
        await Assert.That(X(clear)).IsLessThan(X(suffix));
        host.Drive(golden, "click-at-155-28");
        await Assert.That(box.RevealPassword).IsTrue();
    }

    // select.rs cleanable: the clear button clears the selection without opening the list.
    [Test]
    public async Task A_combo_box_clear_button_clears_the_selection()
    {
        var golden = Case("select/cleanable-pointer.base/at-199-24/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var box = (ComboBox)host.Control;
        var clear = Part<Button>(box, "PART_ClearButton");
        var caret = Part<PathIcon>(box, "PART_Caret");
        await Assert.That(clear.IsEffectivelyVisible).IsTrue();
        await Assert.That(caret.IsEffectivelyVisible).IsFalse();
        host.Drive(golden, "click-at-199-24");
        await Assert.That(box.SelectedIndex).IsEqualTo(-1);
        await Assert.That(box.IsDropDownOpen).IsFalse();
        await Assert.That(clear.IsEffectivelyVisible).IsFalse();
        await Assert.That(caret.IsEffectivelyVisible).IsTrue();
        box.SelectedIndex = 0;
        host.Flush();
        await Assert.That(clear.IsEffectivelyVisible).IsTrue();
    }
}
