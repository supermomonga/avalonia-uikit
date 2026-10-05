using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Builds the Avalonia control for a golden case: the same parameters the
/// GPUI reference harness used (reference/src/cases/*.rs), mapped to the
/// theme's style classes and the control's own properties.
/// </summary>
public static partial class Adapters
{
    public static Control Create(GoldenCase c) => c.Component switch
    {
        "surface" => new Border { Width = 1, Height = 1 },
        "button" => Button(c),
        "button-loading" => LoadingButton(c),
        "dropdownbutton" => DropDownButtonCase(c),
        "buttongroup" => ButtonGroup(c),
        "uikit-buttongroup" => ButtonGroupCase(c),
        "toggle" => Toggle(c),
        "togglegroup" => ToggleGroup(c),
        "uikit-togglegroup" => ToggleGroupCase(c),
        "checkbox" => Check(new CheckBox(), c),
        "radio" => Check(new RadioButton(), c),
        "switch" => Check(new ToggleSwitch(), c),
        "dropdown" => new Button
        {
            Content = c.Str("label", "Open"),
            Flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft, ItemsSource = StandardMenu() },
        },
        "context" => ContextArea(),
        "split" => Split(c),
        "split-loading" => LoadingSplit(c),
        "menubar" => MenuBar(),
        "number" => NumberAffixes(Number(c), c),
        "groupbox" => Slotted(GroupBox(c), c),
        "separator" => Slotted(Separator(c), c),
        "link" => new HyperlinkButton { Content = c.Str("label", "Documentation") },
        "progress" => Progress(c),
        "progress-circle" => Slotted(ProgressCircle(c), c),
        "spinner" => Slotted(Spinner(c), c),
        "tooltip" => Tooltip(c),
        "scroll" => Scroll(c),
        "icon" => IconCase(c),
        "uikit-icon" => IconKindCase(c),
        "label" => LabelCase(c),
        "input" => InputFeatures(Input(c), c),
        "input-menu" => InputFeatures(Input(c), c),
        "uikit-label" => TextLabelCase(c),
        "textarea" => Textarea(c),
        "input-group" => InputGroup(c),
        "input-group-loading" => LoadingInputGroup(c),
        "list" => List(c),
        "select" => SelectFeatures(Select(c), c),
        "virtual" => VirtualListCase(c),
        "combobox" => Select(c, "combobox"),
        "uikit-select" => SelectCase(c, combobox: false),
        "uikit-combobox" => SelectCase(c, combobox: true),
        "uikit-input" => InputRulesCase(c),
        "uikit-number" => NumberInputCase(c),
        "uikit-inputgroup" => InputGroupCase(c),
        "uikit-slider" => RangeSliderCase(c),
        "tree" => Tree(c),
        "uikit-list" => ListViewCase(c),
        "uikit-tree" => TreeCase(c),
        "pagination" => Pagination(c),
        "uikit-pagination" => PaginationCase(c),
        "tabs" => TabStrip(c),
        "toolbar" => Toolbar(c),
        "uikit-toolbar" => ToolbarCase(c),
        "popover" => Popover(c),
        "accordion" => Accordion(c),
        "uikit-accordion" => AccordionCase(c),
        "notification" => NotificationArea(c),
        "resizable" => Resizable(c),
        "image" => ImageCase(c),
        "uikit-image" => AsyncImageCase(c),
        "calendar" => CalendarCase(c),
        "datepicker" => DatePicker(c),
        "timefield" => TimeField(c),
        "uikit-calendar" => UikitCalendar(c),
        "uikit-timefield" => UikitTimeField(c),
        "uikit-datepicker" => UikitDateField(c),
        "collapsible" => Collapsible(c),
        "slider" => Slider(c),
        "table" => TableRows(Table(c), c),
        "datatable" => TableRows(DataTable(c), c),
        "datagrid" => DataGridCells(DataGridCase(c), c),
        "tabalonia" => TabaloniaCase(c),
        "dock" => DockCase(c),
        "carousel" => CarouselCase(c),
        "sidebar" => c.Str("host", "splitview") == "drawer" ? SidebarDrawer(c) : Sidebar(c),
        "sheet" => Sheet(c),
        "color_picker" => ColorPickerCase(c),
        "titlebar" => new DecorationsHost(c.Params.ContainsKey("title") ? c.Str("title", "") : null)
        {
            Width = c.Num("width", 480),
            Height = 34,
            HidesCaptionButtons = true,
            // macOS: GPUI keeps 80px for the traffic lights.
            Resources = { ["UIKit.TitleBar.Padding"] = new Avalonia.Thickness(80, 0, 0, 1) },
        },
        _ => KitControl(c) ?? throw new NotSupportedException($"no adapter for {c.Component}"),
    };

    /// <summary>
    /// Undoes behavior of a control's own logic that the theme does not own and
    /// GPUI does not have (R24): NumericUpDown selects its text when a press on a
    /// step button focuses it. The caret is hidden: its blink phase at capture
    /// time is not part of the look (R25).
    /// </summary>
    public static void AfterDrive(GoldenCase c, CaseHost host)
    {
        // The case's "today" (GPUI's is pinned by the reference patch); Avalonia reads
        // DateTime.Today whenever it lays out a month, so pin it after the last step.
        if (c.Component is "calendar" or "datepicker" && Date(c, "today", "2025-06-10") is { } today)
        {
            foreach (var day in host.Window.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.CalendarDayButton>())
            {
                SetIsToday(day, day.DataContext is DateTime d && d.Date == today);
            }
        }
        // R28: Avalonia selects a menu's first item when the menu opens; GPUI starts
        // with none. Only the pointer's item (and an open submenu's) stays selected.
        if (!c.State.Contains("key-", StringComparison.Ordinal))
        {
            foreach (var item in host.Window.GetVisualDescendants().OfType<MenuItem>())
            {
                if (item.IsSelected && !item.IsPointerOver && !item.IsSubMenuOpen)
                {
                    item.IsSelected = false;
                }
            }
        }
        // R28: DataGrid selects its first row when it takes focus; GPUI's table selects nothing.
        if (c.Component == "datagrid" && !c.State.Contains("click", StringComparison.Ordinal))
        {
            foreach (var grid in host.Window.GetVisualDescendants().OfType<DataGrid>())
            {
                grid.SelectedIndex = -1;
            }
        }
        if (c.Component == "dock")
        {
            PlaceDockDragPreview(c, host);
        }
        foreach (var box in host.Window.GetVisualDescendants().OfType<TextBox>())
        {
            // R24: Tab selects all of a single-line TextBox's text, GPUI selects nothing.
            if (c.Bool("select_all"))
            {
                box.SelectAll();
            }
            else
            {
                box.ClearSelection();
            }
            box.CaretBrush = Avalonia.Media.Brushes.Transparent;
        }
        host.Flush();
    }

    /// <summary>ScrollbarMode::Always / Hover / Scrolling.</summary>
    private static ScrollViewer Scroll(GoldenCase c)
    {
        var mode = c.Str("scrollbar_mode", "hover");
        var viewer = new ScrollViewer
        {
            Width = c.Num("width", 160),
            Height = c.Num("height", 100),
            AllowAutoHide = mode != "always",
            Content = new Border { Width = c.Num("width", 160), Height = c.Num("content", 400) },
        };
        Scrollbars.SetShowOnHover(viewer, mode != "scrolling");
        return viewer;
    }

    /// <summary>A TextBox as GPUI's Input: value or placeholder, prefix icon, suffix button, masking.</summary>
    private static TextBox Input(GoldenCase c)
    {
        var box = new TextBox
        {
            Text = c.Has("value") ? c.Str("value") : null,
            PlaceholderText = c.Has("placeholder") ? c.Str("placeholder") : null,
            Width = c.Num("width", 200),
            IsEnabled = !c.Bool("disabled"),
            IsReadOnly = c.Bool("readonly"),
        };
        ClassFrom(box, c, "size", "medium");
        if (c.Bool("masked"))
        {
            box.PasswordChar = '•';
        }
        if (c.Bool("mask_toggle"))
        {
            box.Classes.Add("revealPasswordButton");
        }
        if (c.Has("prefix"))
        {
            var icon = Icon(c.Str("prefix"));
            icon.Classes.Add("small");
            box.InnerLeftContent = icon;
        }
        if (c.Has("suffix"))
        {
            box.InnerRightContent = new Button { Classes = { "ghost", "xsmall", "icon-only" }, Content = Icon(c.Str("suffix")) };
        }
        return box;
    }

    /// <summary>A multi-line TextBox as GPUI's Textarea: rows, auto-grow range or a fixed height.</summary>
    private static TextBox Textarea(GoldenCase c)
    {
        var box = new TextBox
        {
            AcceptsReturn = true,
            Text = c.Has("value") ? c.Str("value") : null,
            PlaceholderText = c.Has("placeholder") ? c.Str("placeholder") : null,
            Width = c.Num("width", 220),
            IsEnabled = !c.Bool("disabled"),
            IsReadOnly = c.Bool("readonly"),
        };
        ClassFrom(box, c, "size", "medium");
        if (c.Has("max_rows"))
        {
            box.MinLines = (int)c.Num("min_rows", 1);
            box.MaxLines = (int)c.Num("max_rows", 1);
        }
        else if (c.Has("height"))
        {
            box.Height = c.Num("height", 0);
        }
        else
        {
            box.MinLines = box.MaxLines = (int)c.Num("rows", 1);
        }
        return box;
    }

    /// <summary>The fruit names GPUI's list cases show, then "Item N".</summary>
    public static IEnumerable<string> Names(int count)
    {
        string[] names = ["Apple", "Banana", "Cherry", "Grape", "Lemon", "Mango", "Orange", "Peach", "Pear", "Plum", "Kiwi", "Lime"];
        return Enumerable.Range(0, count).Select(i => i < names.Length ? names[i] : $"Item {i + 1}");
    }

    /// <summary>A ListBox as GPUI's List: rows, a selected row and a disabled row.</summary>
    private static ListBox List(GoldenCase c)
    {
        var list = new ListBox { Width = c.Num("width", 240), Height = c.Num("height", 200) };
        if (!c.Bool("empty"))
        {
            var disabled = (int)c.Num("disabled_row", -1);
            var i = 0;
            foreach (var name in Names((int)c.Num("count", 5)))
            {
                list.Items.Add(new ListBoxItem { Content = name, IsEnabled = i++ != disabled });
            }
        }
        if (c.Num("selected", -1) is var selected and >= 0)
        {
            list.SelectedIndex = (int)selected;
        }
        return list;
    }

    /// <summary>
    /// GPUI's bare VirtualList as a ListBox with unstyled containers (app
    /// content): "Row N" rows 30/45/60px tall (34 when uniform), every other one
    /// on secondary, scrolled to `offset`.
    /// </summary>
    private static ListBox VirtualList(GoldenCase c)
    {
        var uniform = c.Bool("uniform");
        double Row(int i) => uniform ? 34 : (i % 3) switch { 0 => 30, 1 => 45, _ => 60 };
        var secondary = (Avalonia.Media.IBrush)Avalonia.Application.Current!.FindResource(c.Variant, "UIKit.Secondary")!;
        var bare = new ControlTheme(typeof(ListBoxItem))
        {
            Setters =
            {
                new Setter(Avalonia.Controls.Primitives.TemplatedControl.TemplateProperty, new Avalonia.Controls.Templates.FuncControlTemplate<ListBoxItem>((item, scope) =>
                    new Avalonia.Controls.Presenters.ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [!Avalonia.Controls.Presenters.ContentPresenter.ContentProperty] = item[!ContentControl.ContentProperty],
                        [!Avalonia.Controls.Presenters.ContentPresenter.ContentTemplateProperty] = item[!ContentControl.ContentTemplateProperty],
                    })),
            },
        };
        var list = new ListBox
        {
            Width = c.Num("width", 240),
            Height = c.Num("height", 200),
            ItemContainerTheme = bare,
            ItemsSource = Enumerable.Range(0, (int)c.Num("count", 1000)).ToList(),
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<int>((i, _) => new Border
            {
                Height = Row(i),
                Background = i % 2 == 1 ? secondary : null,
                Padding = new Avalonia.Thickness(12, 0),
                Child = new TextBlock { Text = $"Row {i}", FontSize = 16, LineHeight = 26, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top },
            }),
        };
        if (c.Num("offset", 0) is var offset and > 0)
        {
            list.TemplateApplied += (_, e) =>
            {
                var viewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer")!;
                void Apply(object? sender, EventArgs args)
                {
                    viewer.LayoutUpdated -= Apply;
                    viewer.Offset = new Avalonia.Vector(0, offset);
                }
                viewer.LayoutUpdated += Apply;
            };
        }
        return list;
    }

    /// <summary>A TreeView with the tree GPUI's case shows; `selected` is a flattened row index.</summary>
    private static TreeView Tree(GoldenCase c)
    {
        TreeViewItem Item(string header, bool expanded = false, bool enabled = true, params TreeViewItem[] children)
        {
            var item = new TreeViewItem { Header = header, IsExpanded = expanded, IsEnabled = enabled };
            foreach (var child in children)
            {
                item.Items.Add(child);
            }
            return item;
        }
        var components = Item("components", true, true, Item("button.rs"), Item("tree.rs"));
        var src = Item("src", true, true, components, Item("lib.rs"));
        var assets = Item("assets", false, false, Item("logo.svg"));
        var cargo = Item("Cargo.toml");
        var tree = new TreeView { Width = c.Num("width", 240), Height = c.Num("height", 238), Items = { src, assets, cargo } };
        TreeViewItem[] flat = [src, components, (TreeViewItem)components.Items[0]!, (TreeViewItem)components.Items[1]!, (TreeViewItem)src.Items[1]!, assets, cargo];
        if (c.Num("selected", -1) is var selected and >= 0)
        {
            tree.SelectedItem = flat[(int)selected];
        }
        if (c.Bool("rounded"))
        {
            tree.Padding = new Avalonia.Thickness(4);
            tree.BorderThickness = new Avalonia.Thickness(1);
            tree.BorderBrush = (Avalonia.Media.IBrush)Avalonia.Application.Current!.FindResource(c.Variant, "UIKit.Border")!;
            tree.CornerRadius = new Avalonia.CornerRadius(5.5);
            foreach (var item in flat)
            {
                item.CornerRadius = new Avalonia.CornerRadius(6);
            }
        }
        return tree;
    }

    /// <summary>A ComboBox as GPUI's Select: fruits, a selected and a disabled one, a placeholder.</summary>
    private static ComboBox Select(GoldenCase c, string? look = null)
    {
        var box = new ComboBox
        {
            Width = c.Num("width", 200),
            PlaceholderText = c.Str("placeholder", "Select a fruit"),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(box, c, "size", "medium");
        if (look is not null)
        {
            box.Classes.Add(look);
        }
        var disabled = (int)c.Num("disabled_row", -1);
        var i = 0;
        foreach (var name in Names((int)c.Num("count", 4)))
        {
            box.Items.Add(new ComboBoxItem { Content = name, IsEnabled = i++ != disabled });
        }
        if (c.Num("selected", -1) is var selected and >= 0)
        {
            box.SelectedIndex = (int)selected;
        }
        return box;
    }

    /// <summary>A TextBox.group as GPUI's InputGroup: addons before and after the text.</summary>
    private static TextBox InputGroup(GoldenCase c)
    {
        var box = c.Bool("multiline") ? Textarea(c) : Input(c);
        box.Classes.Add("group");
        if (c.Bool("multiline"))
        {
            box.MinLines = box.MaxLines = (int)c.Num("rows", 3);
        }
        if (c.Has("start"))
        {
            var start = c.Str("start");
            box.InnerLeftContent = start == "search" ? Icon(start) : start;
        }
        var tail = new List<object>();
        if (c.Has("end"))
        {
            tail.Add(c.Str("end"));
        }
        if (c.Has("end_button"))
        {
            tail.Add(new Button { Content = c.Str("end_button") });
        }
        if (c.Has("end_icon"))
        {
            var button = new Button { Content = Icon(c.Str("end_icon")), Classes = { "icon-only" } };
            if (c.Str("button_size", "xsmall") == "small")
            {
                button.Classes.Add("small");
            }
            tail.Add(button);
        }
        box.InnerRightContent = tail.Count switch
        {
            0 => null,
            1 => tail[0],
            _ => new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Children = { } },
        };
        if (c.Bool("invalid"))
        {
            DataValidationErrors.SetError(box, new Exception("invalid"));
        }
        return box;
    }

    /// <summary>Text as GPUI's Label: TextBlock.label, SelectableTextBlock.label or a Label.</summary>
    private static Control LabelCase(GoldenCase c)
    {
        TextBlock MakeText(TextBlock text)
        {
            text.Inlines!.Add(new Avalonia.Controls.Documents.Run(c.Str("text", "Label")));
            if (c.Has("secondary"))
            {
                text.Inlines.Add(new Avalonia.Controls.Documents.Run(" " + c.Str("secondary")) { Classes = { "secondary" } });
            }
            return text;
        }
        var control = c.Str("control", "");
        Control result = control switch
        {
            "label" => new Label { Content = MakeText(new TextBlock()) },
            "selectable" => MakeText(new SelectableTextBlock { Classes = { "label" } }),
            _ => MakeText(new TextBlock { Classes = { "label" } }),
        };
        var size = c.Str("text_size", "base") switch { "xs" => 12, "sm" => 14, "lg" => 18, "xl" => 20, "2xl" => 24, _ => 16 };
        result.SetValue(TextElement.FontSizeProperty, (double)size);
        result.SetValue(TextElement.FontWeightProperty, c.Str("weight", "normal") switch
        {
            "medium" => Avalonia.Media.FontWeight.Medium,
            "semibold" => Avalonia.Media.FontWeight.SemiBold,
            "bold" => Avalonia.Media.FontWeight.Bold,
            _ => Avalonia.Media.FontWeight.Normal,
        });
        if (c.Has("width"))
        {
            result.Width = c.Num("width", 0);
            result.SetValue(TextBlock.TextAlignmentProperty, c.Str("align", "left") switch
            {
                "center" => Avalonia.Media.TextAlignment.Center,
                "right" => Avalonia.Media.TextAlignment.Right,
                _ => Avalonia.Media.TextAlignment.Left,
            });
        }
        if (c.Str("leading", "default") == "relaxed")
        {
            result.SetValue(TextBlock.LineHeightProperty, 28.8);
        }
        if (c.Has("color"))
        {
            var key = c.Str("color") == "muted" ? "UIKit.MutedForeground" : "UIKit.Danger";
            result.SetValue(TextElement.ForegroundProperty, (Avalonia.Media.IBrush)Avalonia.Application.Current!.FindResource(c.Variant, key)!);
        }
        return result;
    }

    /// <summary>A PathIcon with the theme's geometry, size class, color and rotation.</summary>
    private static PathIcon IconCase(GoldenCase c)
    {
        var icon = Icon(c.Str("icon"));
        if (c.Has("size"))
        {
            ClassFrom(icon, c, "size", "medium");
        }
        if (c.Has("color"))
        {
            var key = "UIKit." + string.Concat(c.Str("color").Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
            icon.Foreground = (Avalonia.Media.IBrush)Avalonia.Application.Current!.FindResource(c.Variant, key)!;
        }
        if (c.Has("rotate"))
        {
            icon.RenderTransform = new Avalonia.Media.RotateTransform(c.Num("rotate", 0));
        }
        return icon;
    }

    /// <summary>A Lucide icon from the theme, as an app would put one in content.</summary>
    public static PathIcon Icon(string name)
    {
        var key = "UIKit.Icon." + string.Concat(name.Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
        return new PathIcon { Data = (Avalonia.Media.Geometry)Avalonia.Application.Current!.FindResource(key)! };
    }

    /// <summary>Adds the case's value of <paramref name="key"/> as a class unless it is the default.</summary>
    public static void ClassFrom(Control control, GoldenCase c, string key, string defaultValue)
    {
        var value = c.Str(key, defaultValue);
        if (value != defaultValue)
        {
            control.Classes.Add(value);
        }
    }

    public static void FlagClass(Control control, GoldenCase c, string key, string? className = null)
    {
        if (c.Bool(key))
        {
            control.Classes.Add(className ?? key);
        }
    }

    /// <summary>The menu every popup case shows (reference/src/cases/menu.rs standard_menu).</summary>
    public static List<Control> StandardMenu() =>
    [
        new MenuItem { Header = "New File" },
        new MenuItem { Header = "Refresh", InputGesture = new KeyGesture(Key.F5) },
        new MenuItem { Header = "Word Wrap", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
        new Separator(),
        new MenuItem { Header = "Rename", IsEnabled = false },
        new MenuItem { Header = "Edit", ItemsSource = new List<Control> { new MenuItem { Header = "Copy" }, new MenuItem { Header = "Paste" } } },
    ];

    private static ProgressBar Progress(GoldenCase c)
    {
        var value = c.Num("value", 40);
        var valueTo = c.Num("value_to", value);
        var progress = new ProgressBar
        {
            Width = c.Num("width", 200),
            Value = value,
            IsIndeterminate = c.Bool("loading"),
        };
        ClassFrom(progress, c, "size", "medium");
        // The case's click switches the value, as the GPUI case's wrapper does.
        progress.PointerReleased += (_, _) => progress.Value = progress.Value == value ? valueTo : value;
        return progress;
    }

    private static ProgressBar ProgressCircle(GoldenCase c)
    {
        var value = c.Num("value", 40);
        var valueTo = c.Num("value_to", value);
        var progress = new ProgressBar
        {
            Theme = (ControlTheme)Avalonia.Application.Current!.FindResource("UIKitProgressCircle")!,
            Value = value,
            IsIndeterminate = c.Bool("loading"),
        };
        ClassFrom(progress, c, "size", "medium");
        if (c.Num("side", 0) is > 0 and var side)
        {
            progress.Width = side;
            progress.Height = side;
        }
        progress.PointerReleased += (_, _) => progress.Value = progress.Value == value ? valueTo : value;
        return progress;
    }

    private static Button Tooltip(GoldenCase c)
    {
        var button = new Button { Content = c.Str("label", "Hover me") };
        ClassFrom(button, c, "size", "medium");
        ToolTip.SetTip(button, c.Str("tip", "Tooltip text"));
        return button;
    }

    private static ProgressBar Spinner(GoldenCase c)
    {
        var spinner = new ProgressBar { IsIndeterminate = true };
        spinner.Theme = (Avalonia.Styling.ControlTheme)Avalonia.Application.Current!.FindResource("UIKitSpinner")!;
        ClassFrom(spinner, c, "size", "medium");
        return spinner;
    }

    private static GroupBox GroupBox(GoldenCase c)
    {
        var group = new GroupBox { Content = c.Str("content", "Content"), Width = c.Num("width", 240) };
        if (c.Has("title"))
        {
            group.Header = c.Str("title");
        }
        ClassFrom(group, c, "variant", "normal");
        return group;
    }

    private static Separator Separator(GoldenCase c)
    {
        var separator = new Separator();
        var length = c.Num("length", 160);
        if (c.Bool("vertical"))
        {
            separator.Classes.Add("vertical");
            separator.Height = length;
        }
        else
        {
            separator.Width = length;
        }
        FlagClass(separator, c, "dashed");
        return separator;
    }

    private static NumericUpDown Number(GoldenCase c)
    {
        var number = new NumericUpDown
        {
            Value = decimal.Parse(c.Str("value", "42"), System.Globalization.CultureInfo.InvariantCulture),
            Width = c.Num("width", 160),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(number, c, "size", "medium");
        return number;
    }

    private static Border ContextArea()
    {
        var area = new Border
        {
            Width = 120,
            Height = 48,
            BorderThickness = new Avalonia.Thickness(1),
            Background = Avalonia.Media.Brushes.Transparent,
            ContextMenu = new ContextMenu { ItemsSource = StandardMenu() },
        };
        area.Bind(Border.BorderBrushProperty, area.GetResourceObservable("UIKit.Border"));
        return area;
    }

    private static SplitButton Split(GoldenCase c)
    {
        var split = new SplitButton
        {
            Content = c.Str("label", "Save"),
            Flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight, ItemsSource = StandardMenu() },
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(split, c, "variant", "default");
        ClassFrom(split, c, "size", "medium");
        FlagClass(split, c, "outline");
        FlagClass(split, c, "selected");
        return split;
    }

    private static Menu MenuBar() => new()
    {
        ItemsSource = new List<MenuItem>
        {
            new()
            {
                Header = "File",
                ItemsSource = new List<Control>
                {
                    new MenuItem { Header = "New File" },
                    new MenuItem { Header = "Refresh", InputGesture = new KeyGesture(Key.F5) },
                    new Separator(),
                    new MenuItem { Header = "Quit" },
                },
            },
            new() { Header = "Edit", ItemsSource = new List<MenuItem> { new() { Header = "Undo" }, new() { Header = "Redo" } } },
            new() { Header = "View", ItemsSource = new List<MenuItem> { new() { Header = "Zoom" } } },
        },
    };

    private static ToggleButton Check(ToggleButton control, GoldenCase c)
    {
        if (c.Has("label"))
        {
            control.Content = c.Str("label");
        }
        control.IsChecked = c.Bool("checked");
        control.IsEnabled = !c.Bool("disabled");
        ClassFrom(control, c, "size", "medium");
        return control;
    }

    private static ToggleButton Toggle(GoldenCase c)
    {
        var toggle = new ToggleButton
        {
            Content = c.Has("label") ? c.Str("label") : Icon(c.Str("icon")),
            IsChecked = c.Bool("checked"),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(toggle, c, "variant", "ghost");
        ClassFrom(toggle, c, "size", "medium");
        return toggle;
    }

    /// <summary>A ListBox.toggle-group; '1's in `checked` mark the selected items.</summary>
    private static Slider Slider(GoldenCase c)
    {
        var slider = new Slider
        {
            Minimum = c.Num("min", 0),
            Maximum = c.Num("max", 100),
            Value = c.Num("value", 40),
            IsEnabled = !c.Bool("disabled"),
        };
        if (c.Bool("vertical"))
        {
            slider.Orientation = Orientation.Vertical;
        }
        else
        {
            slider.Width = c.Num("width", 200);
        }
        FlagClass(slider, c, "reverse");
        return slider;
    }

    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_IsToday")]
    private static extern void SetIsToday(Avalonia.Controls.Primitives.CalendarDayButton button, bool value);

    private static DateTime? Date(GoldenCase c, string key, string fallback) =>
        DateTime.TryParseExact(c.Str(key, fallback), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d) ? d : null;

    private static TimePicker TimeField(GoldenCase c)
    {
        var picker = new TimePicker
        {
            SelectedTime = TimeSpan.Parse(c.Str("time", "09:30:15"), System.Globalization.CultureInfo.InvariantCulture),
            ClockIdentifier = c.Str("cycle", "h23") == "h12" ? "12HourClock" : "24HourClock",
            UseSeconds = c.Str("precision", "minute") == "second",
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(picker, c, "size", "medium");
        if (c.Bool("invalid"))
        {
            DataValidationErrors.SetError(picker, new Exception("invalid"));
        }
        return picker;
    }

    // GPUI's text and placeholder are the app's to set (usage contract).
    private static CalendarDatePicker DatePicker(GoldenCase c)
    {
        var picker = new CalendarDatePicker
        {
            Width = c.Num("width", 220),
            HorizontalAlignment = HorizontalAlignment.Left,
            SelectedDateFormat = CalendarDatePickerFormat.Custom,
            CustomDateFormatString = "yyyy/MM/dd",
            PlaceholderText = "Select date",
            FirstDayOfWeek = DayOfWeek.Sunday,
            SelectedDate = Date(c, "date", "none"),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(picker, c, "size", "medium");
        return picker;
    }

    private static Calendar CalendarCase(GoldenCase c)
    {
        var date = Date(c, "date", "2025-03-14");
        var today = Date(c, "today", "2025-06-10")!.Value;
        var calendar = new Calendar
        {
            DisplayDate = date ?? today,
            SelectedDate = date,
            FirstDayOfWeek = c.Str("first_day", "sun") == "mon" ? DayOfWeek.Monday : DayOfWeek.Sunday,
        };
        ClassFrom(calendar, c, "size", "medium");
        if (Date(c, "blackout_from", "none") is { } from && Date(c, "blackout_to", "none") is { } to)
        {
            calendar.BlackoutDates.Add(new CalendarDateRange(from, to));
        }
        if (c.Str("view", "day") == "month")
        {
            calendar.DisplayMode = CalendarMode.Year;
        }
        return calendar;
    }

    // reference/src/cases/color_picker.rs: the swatch, or ColorSelect (the field class) 200px wide.
    private static ColorPicker ColorPickerCase(GoldenCase c)
    {
        var picker = new ColorPicker { Color = Avalonia.Media.Color.Parse("#" + c.Str("value", "2563EB")) };
        if (c.Params.ContainsKey("label"))
        {
            picker.Content = c.Str("label", "");
        }
        ClassFrom(picker, c, "size", "medium");
        if (c.Bool("field"))
        {
            picker.Classes.Add("field");
            picker.Width = c.Num("width", 200);
        }
        return picker;
    }

    /// <summary>
    /// reference/src/cases/sheet.rs: a DrawerPage the size of the window with
    /// the sheet class; tapping the page opens the drawer (a tap, so the
    /// release after a backdrop press does not reopen it).
    /// </summary>
    private static DrawerPage Sheet(GoldenCase c)
    {
        Border Fill(string brush, double width, double height) => new()
        {
            Width = width,
            Height = height,
            HorizontalAlignment = double.IsNaN(width) ? HorizontalAlignment.Stretch : HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(brush),
        };
        var content = new Border { Background = Avalonia.Media.Brushes.Transparent };
        var drawer = new DrawerPage
        {
            Classes = { "sheet" },
            Width = c.Num("width", 560),
            Height = c.Num("height", 400),
            DrawerBehavior = DrawerBehavior.Flyout,
            DrawerPlacement = c.Str("placement", "right") switch
            {
                "left" => DrawerPlacement.Left,
                "top" => DrawerPlacement.Top,
                "bottom" => DrawerPlacement.Bottom,
                _ => DrawerPlacement.Right,
            },
            Drawer = Fill("UIKit.Muted", double.NaN, 40),
            Content = content,
        };
        if (c.Params.ContainsKey("size"))
        {
            drawer.DrawerLength = c.Num("size", 350);
        }
        if (c.Params.ContainsKey("title"))
        {
            drawer.DrawerHeader = c.Str("title", "");
        }
        if (c.Bool("footer"))
        {
            drawer.DrawerFooter = Fill("UIKit.Primary", 80, 24);
        }
        if (c.Bool("no_overlay"))
        {
            drawer.BackdropBrush = null;
        }
        content.Tapped += (_, _) => drawer.IsOpen = true;
        return drawer;
    }

    // reference/src/cases/sidebar.rs: the muted content area beside the sidebar.
    private static Border SidebarContent() => new()
    {
        [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.Muted"),
    };

    /// <summary>
    /// GPUI's Sidebar modes on SplitView: Icon is CompactInline, Offcanvas and
    /// None are Inline (None always open). Releasing the pointer on the content
    /// toggles the pane, as a click toggles collapsed in the GPUI case.
    /// </summary>
    private static SplitView Sidebar(GoldenCase c)
    {
        var collapsible = c.Str("collapsible", "icon");
        var content = SidebarContent();
        var split = new SplitView
        {
            Width = c.Num("width", 400),
            Height = c.Num("height", 160),
            DisplayMode = collapsible == "icon" ? SplitViewDisplayMode.CompactInline : SplitViewDisplayMode.Inline,
            PanePlacement = c.Str("side", "left") == "right" ? SplitViewPanePlacement.Right : SplitViewPanePlacement.Left,
            IsPaneOpen = collapsible == "none" || !c.Bool("collapsed"),
            Content = content,
        };
        if (c.Params.ContainsKey("sidebar_width"))
        {
            split.OpenPaneLength = c.Num("sidebar_width", 255);
        }
        content.PointerReleased += (_, _) => split.IsPaneOpen = !split.IsPaneOpen;
        return split;
    }

    // DrawerPage: Locked for GPUI's non-collapsible sidebar, CompactInline for Icon.
    private static DrawerPage SidebarDrawer(GoldenCase c)
    {
        Border Block() => new()
        {
            Width = 24,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Left,
            [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.SidebarPrimary"),
        };
        var icon = c.Str("collapsible", "icon") == "icon";
        var drawer = new DrawerPage
        {
            Width = c.Num("width", 400),
            Height = c.Num("height", 160),
            DrawerBehavior = icon ? DrawerBehavior.Auto : DrawerBehavior.Locked,
            DrawerLayoutBehavior = icon ? DrawerLayoutBehavior.CompactInline : DrawerLayoutBehavior.Split,
            IsOpen = !icon || !c.Bool("collapsed"),
            Content = SidebarContent(),
        };
        if (c.Bool("blocks"))
        {
            drawer.DrawerHeader = Block();
            drawer.DrawerFooter = Block();
        }
        return drawer;
    }

    /// <summary>
    /// reference/src/cases/carousel.rs: numbered slides, Previous and Next 16px
    /// outside them, the pages 16px below (the usage the theme documents).
    /// </summary>
    private static StackPanel CarouselCase(GoldenCase c)
    {
        var count = (int)c.Num("count", 3);
        var height = c.Num("height", 120);
        var carousel = new Carousel
        {
            Width = c.Num("width", 240),
            Focusable = true,
            ItemsSource = Enumerable.Range(1, count).Select(i => Slide(i, height)).ToList(),
            SelectedIndex = (int)c.Num("selected", 0),
        };
        var size = c.Str("size", "medium");
        var square = size switch { "xsmall" => 20, "small" => 24, _ => 32 };
        Button Nav(string icon, bool next)
        {
            var button = new Button
            {
                Classes = { "outline", "icon-only", "rounded-full" },
                Content = Icon(icon),
                HorizontalAlignment = next ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = next ? new Avalonia.Thickness(0, 0, -16 - square, 0) : new Avalonia.Thickness(-16 - square, 0, 0, 0),
            };
            ClassFrom(button, c, "size", "medium");
            button.Click += (_, _) =>
            {
                if (next)
                {
                    carousel.Next();
                }
                else
                {
                    carousel.Previous();
                }
            };
            return button;
        }
        var previous = Nav("chevron-left", next: false);
        var nextButton = Nav("chevron-right", next: true);
        var pager = new PipsPager
        {
            Classes = { "carousel" },
            NumberOfPages = count,
            SelectedPageIndex = carousel.SelectedIndex,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        void Sync()
        {
            previous.IsEnabled = carousel.SelectedIndex > 0;
            nextButton.IsEnabled = carousel.SelectedIndex < count - 1;
            pager.SelectedPageIndex = carousel.SelectedIndex;
        }
        carousel.SelectionChanged += (_, _) => Sync();
        pager.PropertyChanged += (_, e) =>
        {
            if (e.Property == PipsPager.SelectedPageIndexProperty)
            {
                carousel.SelectedIndex = pager.SelectedPageIndex;
            }
        };
        Sync();
        return new StackPanel
        {
            Spacing = 16,
            Width = carousel.Width,
            Children = { new Panel { Children = { carousel, previous, nextButton } }, pager },
        };
    }

    // The story's slide: a muted card with its number in 24px semibold.
    private static Border Slide(int number, double height) => new()
    {
        Height = height,
        CornerRadius = new Avalonia.CornerRadius(7.5),
        BorderThickness = new Avalonia.Thickness(1),
        BackgroundSizing = Avalonia.Media.BackgroundSizing.OuterBorderEdge,
        [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.Muted"),
        [!Border.BorderBrushProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.Border"),
        Child = new TextBlock
        {
            Text = number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            FontSize = 24,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            LineHeight = 39,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };

    /// <summary>reference/src/cases/table.rs: the story's invoices.</summary>
    public sealed record Invoice(string Id, string Method, string Amount);

    public static readonly Invoice[] Invoices =
    [
        new("INV001", "Credit Card", "$250.00"),
        new("INV002", "PayPal", "$150.00"),
        new("INV003", "Bank Transfer", "$350.00"),
        new("INV004", "Credit Card", "$450.00"),
        new("INV005", "PayPal", "$550.00"),
    ];

    // GPUI's cells without a width share the row equally (star columns); fixed_widths
    // gives every column a width that fits, so none shrinks (pixel columns).
    private static TableView Table(GoldenCase c)
    {
        var fixedWidths = c.Bool("fixed_widths");
        GridLength Width(double px) => fixedWidths ? new GridLength(px) : new GridLength(1, GridUnitType.Star);
        var table = new TableView
        {
            Theme = (ControlTheme)Avalonia.Application.Current!.FindResource("UIKitTable")!,
            Width = c.Num("width", 360),
            ItemsSource = Invoices,
            Columns =
            {
                new TableViewColumn { Header = "Invoice", Width = Width(100), Binding = Avalonia.Data.CompiledBinding.Create<Invoice, string>(r => r.Id) },
                new TableViewColumn { Header = "Method", Width = Width(120), Binding = Avalonia.Data.CompiledBinding.Create<Invoice, string>(r => r.Method) },
                new TableViewColumn
                {
                    Header = "Amount",
                    Width = Width(100),
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Binding = Avalonia.Data.CompiledBinding.Create<Invoice, string>(r => r.Amount),
                },
            },
        };
        ClassFrom(table, c, "size", "medium");
        FlagClass(table, c, "stripe");
        if (c.Bool("bordered"))
        {
            table.BorderThickness = new Avalonia.Thickness(1);
            table.CornerRadius = new Avalonia.CornerRadius(5.5);
        }
        return table;
    }

    /// <summary>reference/src/cases/table.rs: the DataTable's people.</summary>
    public sealed record Person(string Name, string Email, string Amount);

    public static readonly Person[] People =
    [
        new("Ada", "ada@example.com", "$250.00"),
        new("Grace", "grace@example.com", "$150.00"),
        new("Linus", "linus@example.com", "$350.00"),
        new("Ken", "ken@example.com", "$450.00"),
        new("Barbara", "barbara@example.com", "$550.00"),
    ];

    // As tall as GPUI's box: the header, the rows and `extra` below them.
    private static TableView DataTable(GoldenCase c)
    {
        var size = c.Str("size", "medium");
        double row = size switch { "xsmall" => 26, "small" => 30, "large" => 40, _ => 32 };
        var bordered = !c.Bool("borderless");
        var table = new TableView
        {
            Width = c.Num("width", 360),
            Height = (bordered ? 2 : 0) + row * (People.Length + 1) + c.Num("extra", 10),
            ItemsSource = People,
            CanUserResizeColumns = !c.Bool("fixed_columns"),
            Columns =
            {
                new TableViewColumn { Header = "Name", Width = new GridLength(120), Binding = Avalonia.Data.CompiledBinding.Create<Person, string>(r => r.Name) },
                new TableViewColumn { Header = "Email", Width = new GridLength(140), Binding = Avalonia.Data.CompiledBinding.Create<Person, string>(r => r.Email) },
                new TableViewColumn
                {
                    Header = "Amount",
                    Width = new GridLength(80),
                    HorizontalContentAlignment = HorizontalAlignment.Right,
                    Binding = Avalonia.Data.CompiledBinding.Create<Person, string>(r => r.Amount),
                },
            },
        };
        ClassFrom(table, c, "size", "medium");
        FlagClass(table, c, "stripe");
        FlagClass(table, c, "borderless");
        return table;
    }

    /// <summary>
    /// The DataTable cases on DataGrid: the same people and columns, sorted by
    /// Amount when the case sorts (as the GPUI delegate orders its rows).
    /// </summary>
    private static DataGrid DataGridCase(GoldenCase c)
    {
        var size = c.Str("size", "medium");
        double row = size switch { "xsmall" => 26, "small" => 30, "large" => 40, _ => 32 };
        var sort = c.Str("sort", "none");
        var rows = new Avalonia.Collections.DataGridCollectionView(People);
        if (sort is "ascending" or "descending")
        {
            rows.SortDescriptions.Add(Avalonia.Collections.DataGridSortDescription.FromPath(nameof(Person.Amount),
                sort == "ascending" ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending));
        }
        double Width(int ix, double fallback) =>
            c.Params["widths"] is System.Text.Json.Nodes.JsonArray w && ix < w.Count ? w[ix]!.GetValue<double>() : fallback;
        var amount = new DataGridTextColumn
        {
            Header = new TextBlock { Text = "Amount", HorizontalAlignment = HorizontalAlignment.Right },
            Width = new DataGridLength(Width(2, 80)),
            SortMemberPath = nameof(Person.Amount),
            Binding = Avalonia.Data.CompiledBinding.Create<Person, string>(r => r.Amount),
        };
        amount.CellStyleClasses.Add("text-right");
        var grid = new DataGrid
        {
            Width = c.Num("width", 360),
            Height = (c.Bool("borderless") ? 0 : 2) + row * (People.Length + 1) + c.Num("extra", 10),
            ItemsSource = rows,
            CanUserSortColumns = sort != "none",
            CanUserResizeColumns = true,
            CanUserReorderColumns = false,
            // GPUI's table only displays: a click selects, it does not edit.
            IsReadOnly = true,
            Columns =
            {
                new DataGridTextColumn { Header = "Name", Width = new DataGridLength(Width(0, 120)), SortMemberPath = nameof(Person.Name), Binding = Avalonia.Data.CompiledBinding.Create<Person, string>(r => r.Name) },
                new DataGridTextColumn { Header = "Email", Width = new DataGridLength(Width(1, 140)), SortMemberPath = nameof(Person.Email), Binding = Avalonia.Data.CompiledBinding.Create<Person, string>(r => r.Email) },
                amount,
            },
        };
        ClassFrom(grid, c, "size", "medium");
        FlagClass(grid, c, "stripe");
        FlagClass(grid, c, "borderless");
        return grid;
    }

    // GPUI's img() box is the element; Avalonia's Image takes the fitted size, so the
    // box is a panel of the case's size around it.
    private static Control ImageCase(GoldenCase c)
    {
        var image = ImageOf(c);
        if (c.Num("width", 96) is > 0 and var width && c.Num("height", 96) is > 0 and var height)
        {
            image.Width = double.NaN;
            image.Height = double.NaN;
            return new Panel { Width = width, Height = height, Children = { image } };
        }
        return image;
    }

    private static Image ImageOf(GoldenCase c)
    {
        var file = c.Str("image", "wide") switch
        {
            "wide" => "wide-192x96.png",
            "tall" => "tall-96x192.png",
            _ => "small-48x48.png",
        };
        var image = new Image { Source = new Avalonia.Media.Imaging.Bitmap(Path.Combine(AvaloniaUIKit.Tests.Infrastructure.Repo.Root, "assets", "images", file)) };
        if (c.Num("width", 96) is > 0 and var width)
        {
            image.Width = width;
        }
        if (c.Num("height", 96) is > 0 and var height)
        {
            image.Height = height;
        }
        // GPUI's ObjectFit as Stretch (Image.axaml).
        switch (c.Str("fit", "contain"))
        {
            case "fill":
                image.Stretch = Avalonia.Media.Stretch.Fill;
                break;
            case "cover":
                image.Stretch = Avalonia.Media.Stretch.UniformToFill;
                break;
            case "scale-down":
                image.StretchDirection = Avalonia.Media.StretchDirection.DownOnly;
                break;
            case "none":
                image.Stretch = Avalonia.Media.Stretch.None;
                image.HorizontalAlignment = HorizontalAlignment.Left;
                image.VerticalAlignment = VerticalAlignment.Top;
                break;
        }
        if (c.Num("radius", 0) == 8)
        {
            image.Classes.Add("rounded-lg");
        }
        return image;
    }

    // Two empty panels in a Grid with a 1px definition for the splitter between them;
    // GPUI's PANEL_MIN_SIZE as the definitions' minimum.
    private static Grid Resizable(GoldenCase c)
    {
        var vertical = c.Bool("vertical");
        var first = c.Num("first", 120);
        var grid = new Grid { Width = c.Num("width", 320), Height = c.Num("height", 120) };
        var splitter = new GridSplitter { ResizeDirection = vertical ? GridResizeDirection.Rows : GridResizeDirection.Columns };
        if (vertical)
        {
            grid.RowDefinitions.Add(new RowDefinition(first, GridUnitType.Pixel) { MinHeight = 100 });
            grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Pixel));
            grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star) { MinHeight = 100 });
            Grid.SetRow(splitter, 1);
        }
        else
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(first, GridUnitType.Pixel) { MinWidth = 100 });
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Pixel));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 100 });
            Grid.SetColumn(splitter, 1);
        }
        grid.Children.Add(splitter);
        return grid;
    }

    private static Avalonia.Controls.Notifications.WindowNotificationManager NotificationArea(GoldenCase c)
    {
        var type = c.Str("type", "info");
        var manager = new Avalonia.Controls.Notifications.WindowNotificationManager
        {
            Width = c.Num("width", 430),
            Height = c.Num("height", 170),
            MaxItems = 10,
            Position = c.Str("placement", "top-right") switch
            {
                "top-left" => Avalonia.Controls.Notifications.NotificationPosition.TopLeft,
                "top-center" => Avalonia.Controls.Notifications.NotificationPosition.TopCenter,
                "bottom-left" => Avalonia.Controls.Notifications.NotificationPosition.BottomLeft,
                "bottom-center" => Avalonia.Controls.Notifications.NotificationPosition.BottomCenter,
                "bottom-right" => Avalonia.Controls.Notifications.NotificationPosition.BottomRight,
                _ => Avalonia.Controls.Notifications.NotificationPosition.TopRight,
            },
        };
        var kind = type switch
        {
            "success" => Avalonia.Controls.Notifications.NotificationType.Success,
            "warning" => Avalonia.Controls.Notifications.NotificationType.Warning,
            "error" => Avalonia.Controls.Notifications.NotificationType.Error,
            _ => Avalonia.Controls.Notifications.NotificationType.Information,
        };
        // Shown as the window opens, as GPUI pushes it on the first frame; no expiry.
        manager.TemplateApplied += (_, _) => manager.Show(
            new Avalonia.Controls.Notifications.Notification(c.Has("title") ? c.Str("title") : null, c.Str("message"), kind),
            kind, TimeSpan.Zero, classes: type == "none" ? ["plain"] : null);
        return manager;
    }

    private static Control Accordion(GoldenCase c)
    {
        var size = c.Str("size", "medium");
        var scope = c.Str("scope");
        var items = new StackPanel { Classes = { "accordion" }, Width = c.Num("width", 280) - (c.Bool("borderless") ? 0 : 2) };
        (string Title, string Body, string Icon)[] rows =
        [
            ("Is it accessible?", "Yes, it is.", "copy"),
            ("Is it styled?", "Yes, by the theme.", "plus"),
            ("Is it animated?", "Yes, with a spring.", "check"),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            object header = rows[i].Title;
            if (c.Bool("icons"))
            {
                // AccordionItem::icon: the item's icon size, 4px (xsmall, small) or 8px before the title.
                var icon = Icon(rows[i].Icon);
                icon.Width = icon.Height = size switch { "xsmall" => 12, "small" => 14, "large" => 24, _ => 16 };
                header = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = size is "xsmall" or "small" ? 4 : 8,
                    Children = { icon, new TextBlock { Text = rows[i].Title, VerticalAlignment = VerticalAlignment.Center } },
                };
            }
            var expander = new Expander
            {
                Header = header,
                Content = rows[i].Body,
                IsExpanded = i == 0 && c.Bool("open_first"),
                IsEnabled = !(scope == "all" || (scope == "item" && i == 1)),
            };
            ClassFrom(expander, c, "size", "medium");
            items.Children.Add(expander);
        }
        if (c.Bool("borderless"))
        {
            items.HorizontalAlignment = HorizontalAlignment.Left;
            return items;
        }
        return new Border { Classes = { "accordion" }, Child = items, HorizontalAlignment = HorizontalAlignment.Left };
    }

    private static Expander Collapsible(GoldenCase c)
    {
        var up = c.Bool("content_first");
        var expander = new Expander
        {
            Theme = (ControlTheme)Avalonia.Application.Current!.FindResource("UIKitCollapsible")!,
            Width = c.Num("width", 240),
            HorizontalAlignment = HorizontalAlignment.Left,
            IsExpanded = c.Bool("open"),
            ExpandDirection = up ? ExpandDirection.Up : ExpandDirection.Down,
            Header = new TextBlock { Text = "Order details", FontSize = 14, FontWeight = Avalonia.Media.FontWeight.Medium, LineHeight = 22.5 },
            Content = new Border
            {
                Margin = up ? new Avalonia.Thickness(0, 0, 0, 8) : new Avalonia.Thickness(0, 8, 0, 0),
                Height = 48,
                BackgroundSizing = Avalonia.Media.BackgroundSizing.OuterBorderEdge,
                CornerRadius = new Avalonia.CornerRadius(5.5),
                BorderThickness = new Avalonia.Thickness(1),
                [!Border.BorderBrushProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.Border"),
                [!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("UIKit.Muted"),
            },
        };
        FlagClass(expander, c, "motion", "reveal");
        return expander;
    }

    private static Button Popover(GoldenCase c)
    {
        var anchor = c.Str("anchor", "top-left");
        var placement = anchor switch
        {
            "top-center" => PlacementMode.Bottom,
            "top-right" => PlacementMode.BottomEdgeAlignedRight,
            "bottom-left" => PlacementMode.TopEdgeAlignedLeft,
            "bottom-center" => PlacementMode.Top,
            "bottom-right" => PlacementMode.TopEdgeAlignedRight,
            "left-center" => PlacementMode.Right,
            "right-center" => PlacementMode.Left,
            _ => PlacementMode.BottomEdgeAlignedLeft,
        };
        var lines = new StackPanel();
        foreach (var line in c.Str("content", "Popover content").Split('|'))
        {
            lines.Children.Add(new TextBlock { Text = line });
        }
        // GPUI never flips a popover; offset(n) replaces the 4px gap.
        var flyout = new Flyout
        {
            Placement = placement,
            PlacementConstraintAdjustment = Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideX |
                Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideY,
            Content = lines,
        };
        if (c.Has("offset"))
        {
            var extra = c.Num("offset", 4) - 4;
            switch (anchor)
            {
                case "bottom-left" or "bottom-center" or "bottom-right":
                    flyout.VerticalOffset = -extra;
                    break;
                case "left-center":
                    flyout.HorizontalOffset = extra;
                    break;
                case "right-center":
                    flyout.HorizontalOffset = -extra;
                    break;
                default:
                    flyout.VerticalOffset = extra;
                    break;
            }
        }
        if (c.Bool("plain"))
        {
            flyout.FlyoutPresenterClasses.Add("plain");
        }
        if (c.Bool("arrow"))
        {
            flyout.FlyoutPresenterClasses.Add("arrow");
        }
        return new Button { Classes = { "outline" }, Content = c.Str("label", "Open"), Flyout = flyout };
    }

    private static CommandBar Toolbar(GoldenCase c)
    {
        var enabled = !c.Bool("disabled");
        var bar = new CommandBar { Content = c.Has("content") ? c.Str("content") : null };
        ClassFrom(bar, c, "size", "small");
        bar.PrimaryCommands!.Add(new CommandBarButton { Icon = Icon("undo-2"), IsEnabled = enabled });
        bar.PrimaryCommands.Add(new CommandBarButton { Icon = Icon("redo-2"), IsEnabled = enabled });
        bar.PrimaryCommands.Add(new CommandBarSeparator());
        bar.PrimaryCommands.Add(new CommandBarButton { Icon = Icon("plus"), Label = "New", IsEnabled = enabled });
        bar.PrimaryCommands.Add(new CommandBarSeparator());
        bar.PrimaryCommands.Add(new CommandBarToggleButton { Label = "B", IsChecked = c.Bool("checked"), IsEnabled = enabled });
        return bar;
    }

    private static readonly (string Label, string Icon)[] TabContents = [("Account", "copy"), ("Profile", "plus"), ("Settings", "check")];

    private static TabStrip TabStrip(GoldenCase c)
    {
        var strip = new TabStrip { Width = c.Num("width", 320) };
        TabClasses(strip, c);
        for (var i = 0; i < TabContents.Length; i++)
        {
            var item = new TabStripItem();
            TabItemLook(item, c, i);
            item.Content = c.Bool("icons") ? Icon(TabContents[i].Icon) : TabContents[i].Label;
            strip.Items.Add(item);
        }
        strip.SelectedIndex = (int)c.Num("selected", 0);
        return strip;
    }

    /// <summary>The tabs case as a TabControl whose tabs have no content: only its strip shows.</summary>
    public static TabControl TabControl(GoldenCase c)
    {
        var tabs = new TabControl { Width = c.Num("width", 320), HorizontalAlignment = HorizontalAlignment.Left };
        TabClasses(tabs, c);
        for (var i = 0; i < TabContents.Length; i++)
        {
            var item = new TabItem();
            TabItemLook(item, c, i);
            item.Header = c.Bool("icons") ? Icon(TabContents[i].Icon) : TabContents[i].Label;
            tabs.Items.Add(item);
        }
        tabs.SelectedIndex = (int)c.Num("selected", 0);
        return tabs;
    }

    private static void TabClasses(Control control, GoldenCase c)
    {
        ClassFrom(control, c, "variant", "tab");
        ClassFrom(control, c, "size", "medium");
    }

    private static void TabItemLook(Control item, GoldenCase c, int index)
    {
        if (c.Bool("icons"))
        {
            item.Classes.Add("icon-only");
        }
        if (c.Params["tab_widths"] is System.Text.Json.Nodes.JsonArray widths && index < widths.Count)
        {
            item.Width = widths[index]!.GetValue<double>();
        }
        if (c.Num("disabled_index", -1) != index)
        {
            return;
        }
        if (c.Num("selected", 0) != index)
        {
            item.IsEnabled = false;
            return;
        }
        // TabStrip and TabControl always select (SelectionMode.AlwaysSelected) and move
        // the selection off a tab that is disabled when it is realized; GPUI keeps it.
        // A selected tab disabled later keeps the selection, so disable it once laid out.
        void Disable(object? sender, EventArgs e)
        {
            item.LayoutUpdated -= Disable;
            item.IsEnabled = false;
        }
        item.LayoutUpdated += Disable;
    }

    private static PipsPager Pagination(GoldenCase c)
    {
        var pager = new PipsPager
        {
            NumberOfPages = (int)c.Num("total", 5),
            SelectedPageIndex = (int)c.Num("current", 3) - 1,
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(pager, c, "size", "medium");
        FlagClass(pager, c, "compact");
        return pager;
    }

    private static ListBox ToggleGroup(GoldenCase c)
    {
        var list = new ListBox
        {
            Classes = { "toggle-group" },
            SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle,
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(list, c, "variant", "ghost");
        ClassFrom(list, c, "size", "medium");
        FlagClass(list, c, "segmented");
        var labels = c.Str("labels", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        var icons = c.Str("icons", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < Math.Max(labels.Length, icons.Length); i++)
        {
            list.Items.Add(i < icons.Length ? Icon(icons[i]) : labels[i]);
        }
        var checkedMask = c.Str("checked", "");
        for (var i = 0; i < checkedMask.Length; i++)
        {
            if (checkedMask[i] == '1')
            {
                list.Selection.Select(i);
            }
        }
        return list;
    }

    /// <summary>A StackPanel.button-group of labelled buttons; '1's in `selected` mark selected ones.</summary>
    private static StackPanel ButtonGroup(GoldenCase c)
    {
        var panel = new StackPanel
        {
            Orientation = c.Str("layout", "horizontal") == "vertical" ? Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal,
            Classes = { "button-group" },
        };
        var selected = c.Str("selected", "");
        var labels = c.Str("labels", "One,Two,Three").Split(',');
        for (var i = 0; i < labels.Length; i++)
        {
            var button = new Button { Content = labels[i], IsEnabled = !c.Bool("disabled") };
            ClassFrom(button, c, "variant", "default");
            ClassFrom(button, c, "size", "medium");
            FlagClass(button, c, "outline");
            FlagClass(button, c, "compact");
            var rounded = c.Str("rounded", "medium");
            if (rounded != "medium")
            {
                button.Classes.Add("rounded-" + rounded);
            }
            if (i < selected.Length && selected[i] == '1')
            {
                button.Classes.Add("selected");
            }
            panel.Children.Add(button);
        }
        return panel;
    }

    private static Button Button(GoldenCase c)
    {
        var button = new Button();
        if (c.Has("label"))
        {
            button.Content = c.Str("label");
        }
        else if (c.Has("icon"))
        {
            button.Content = Icon(c.Str("icon"));
            button.Classes.Add("icon-only");
        }
        ClassFrom(button, c, "variant", "default");
        ClassFrom(button, c, "size", "medium");
        FlagClass(button, c, "outline");
        FlagClass(button, c, "compact");
        FlagClass(button, c, "selected");
        var rounded = c.Str("rounded", "medium");
        if (rounded != "medium")
        {
            button.Classes.Add($"rounded-{rounded}");
        }
        button.IsEnabled = !c.Bool("disabled");
        return button;
    }
}
