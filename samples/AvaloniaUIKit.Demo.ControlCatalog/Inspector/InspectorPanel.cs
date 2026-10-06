using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Inspector;

/// <summary>
/// The property grid: the element of the active demo it edits, the demo's elements as
/// its XAML nests them, the element's style classes, and its properties by the type
/// that declares them. Every change goes to the live control and to the demo's XAML
/// (<see cref="DemoSession"/>).
/// </summary>
public sealed class InspectorPanel : Border
{
    // Which groups are open, by title, kept from one element to the next.
    private static readonly Dictionary<string, bool> Expanded = [];

    private readonly ToggleButton _pick;
    private readonly StackPanel _body;
    private readonly List<PropertyRow> _rows = [];
    private readonly List<(Expander Expander, PropertyGroup Group, StackPanel Rows)> _groups = [];
    private TextBox? _filter;
    private TextBox? _classesText;
    private WrapPanel? _classChips;
    private TreeView? _outline;
    private Control? _target;
    private bool _syncingClasses;

    public InspectorPanel()
    {
        Classes.Add("inspector");
        _pick = new ToggleButton
        {
            Classes = { "small" },
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new PathIcon { Data = Icons.Find("Inspector"), Width = 14, Height = 14 },
                    new TextBlock { Text = "Pick", VerticalAlignment = VerticalAlignment.Center },
                },
            },
        };
        ToolTip.SetTip(_pick, "Pick an element in a demo (or Alt+click one)");
        _pick.IsCheckedChanged += (_, _) => PickingChanged?.Invoke(this, EventArgs.Empty);
        DockPanel.SetDock(_pick, Avalonia.Controls.Dock.Right);

        var header = new Border
        {
            Classes = { "inspector-section" },
            Padding = new Thickness(14, 8, 10, 8),
            Child = new DockPanel
            {
                Children =
                {
                    _pick,
                    new TextBlock { Text = "Properties", FontSize = 13, FontWeight = Avalonia.Media.FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center },
                },
            },
        };
        DockPanel.SetDock(header, Avalonia.Controls.Dock.Top);
        _body = new StackPanel();
        Child = new DockPanel
        {
            Children =
            {
                header,
                new ScrollViewer { Content = _body, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled },
            },
        };
        ShowEmpty();
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Border);

    /// <summary>The demo the grid works on, or null.</summary>
    public DemoSession? Session { get; private set; }

    /// <summary>The element the grid edits, or null.</summary>
    public XamlElement? Element { get; private set; }

    /// <summary>The control the grid edits, or null.</summary>
    public Control? Target => _target;

    /// <summary>Raised when the grid starts editing another control, or none.</summary>
    public event EventHandler? TargetChanged;

    /// <summary>Whether a press in a demo picks the element under the pointer.</summary>
    public bool IsPicking
    {
        get => _pick.IsChecked == true;
        set => _pick.IsChecked = value;
    }

    /// <summary>Raised when <see cref="IsPicking"/> changes.</summary>
    public event EventHandler? PickingChanged;

    /// <summary>Edits <paramref name="element"/> of <paramref name="session"/>'s demo, or nothing.</summary>
    public void Show(DemoSession? session, XamlElement? element)
    {
        if (Session != session)
        {
            if (Session is not null)
            {
                Session.Changed -= OnSessionChanged;
                Session.Recreated -= OnRecreated;
            }
            Session = session;
            if (session is not null)
            {
                session.Changed += OnSessionChanged;
                session.Recreated += OnRecreated;
            }
        }
        if (_target is not null)
        {
            _target.PropertyChanged -= OnTargetChanged;
            _target.Classes.CollectionChanged -= OnTargetClassesChanged;
            _target = null;
        }
        Element = element;
        _rows.Clear();
        _groups.Clear();
        _body.Children.Clear();
        var target = session is not null && element is not null ? session.Map.ControlOf(element) : null;
        if (session is null || element is null || target is null)
        {
            ShowEmpty();
            TargetChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
        _target = target;
        TargetChanged?.Invoke(this, EventArgs.Empty);
        target.PropertyChanged += OnTargetChanged;
        target.Classes.CollectionChanged += OnTargetClassesChanged;

        _body.Children.Add(ElementSection(session, element, target));
        _body.Children.Add(OutlineSection(session, element));
        _body.Children.Add(ClassesSection(session, element, target));
        _filter = new TextBox { Classes = { "small" }, PlaceholderText = "Filter properties", Margin = new Thickness(14, 12, 14, 4), HorizontalAlignment = HorizontalAlignment.Stretch };
        _filter.TextChanged += (_, _) => ApplyFilter();
        _body.Children.Add(_filter);
        foreach (var group in PropertyCatalog.For(target))
        {
            _body.Children.Add(GroupSection(session, element, target, group));
        }
    }

    /// <summary>Stops editing: the page went away.</summary>
    public void Clear() => Show(null, null);

    private void ShowEmpty()
    {
        _body.Children.Clear();
        _body.Children.Add(new EmptyState
        {
            Margin = new Thickness(16, 48),
            Title = "Nothing selected",
            Description = "Click a demo, then pick one of its elements to edit its properties.",
        });
    }

    private Control ElementSection(DemoSession session, XamlElement element, Control target)
    {
        var title = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { new TextBlock { Classes = { "element-name" }, Text = element.Name } },
        };
        if (Note(element) is { } note)
        {
            title.Children.Add(new TextBlock { Classes = { "outline-note" }, Text = note, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 180 });
        }
        var crumbs = new Breadcrumb { HorizontalAlignment = HorizontalAlignment.Left };
        var path = new List<XamlElement>();
        for (var e = element; e is not null; e = e.Parent)
        {
            if (!e.IsPropertyElement && e != session.Document.Root && session.Map.ControlOf(e) is not null)
            {
                path.Insert(0, e);
            }
        }
        foreach (var e in path.TakeLast(4))
        {
            var crumb = new BreadcrumbItem { Content = e.LocalName, FontSize = 12 };
            crumb.Click += (_, _) => Show(session, e);
            crumbs.Items.Add(crumb);
        }
        var demo = new TextBlock { Classes = { "inspector-label" }, Text = $"{session.Component.Title} · {session.Demo.Title ?? "Demo"}" };
        return new Border
        {
            Classes = { "inspector-section" },
            Child = new StackPanel { Spacing = 6, Children = { demo, title, crumbs } },
        };
    }

    private Control OutlineSection(DemoSession session, XamlElement element)
    {
        _outline = new TreeView { MaxHeight = 240, Margin = new Thickness(-6, 4, 0, 0) };
        if (session.Document.Root is { } root)
        {
            foreach (var child in DemoMap.ObjectChildren(root))
            {
                _outline.Items.Add(OutlineItem(session, child));
            }
        }
        _outline.SelectedItem = FindItem(_outline.Items, element);
        _outline.SelectionChanged += (_, _) =>
        {
            if (_outline?.SelectedItem is TreeViewItem { Tag: XamlElement picked } && picked != Element && Session is { } current
                && current.Map.ControlOf(picked) is not null)
            {
                Show(current, picked);
            }
        };
        return Group("Elements", "Elements", true, _outline);
    }

    private static TreeViewItem? FindItem(ItemCollection items, XamlElement element)
    {
        foreach (var item in items.OfType<TreeViewItem>())
        {
            if (item.Tag == element)
            {
                return item;
            }
            if (FindItem(item.Items, element) is { } found)
            {
                item.IsExpanded = true;
                return found;
            }
        }
        return null;
    }

    private static TreeViewItem OutlineItem(DemoSession session, XamlElement element)
    {
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children = { new TextBlock { Classes = { "outline-type" }, Text = element.Name } },
        };
        if (Note(element) is { } note)
        {
            header.Children.Add(new TextBlock { Classes = { "outline-note" }, Text = note, MaxWidth = 160 });
        }
        var mapped = session.Map.ControlOf(element) is not null;
        var item = new TreeViewItem { Header = header, Tag = element, IsExpanded = element.Parent?.Parent is null };
        item.Classes.Set("unmapped", !mapped);
        if (!mapped)
        {
            ToolTip.SetTip(item, "Not a control in the demo's logical tree (a template, a flyout or a tooltip).");
        }
        foreach (var child in DemoMap.ObjectChildren(element))
        {
            item.Items.Add(OutlineItem(session, child));
        }
        return item;
    }

    // What tells an element apart in the outline: its name, content, text or classes.
    private static string? Note(XamlElement element)
    {
        foreach (var name in (string[])["x:Name", "Name", "Content", "Header", "Label", "Title", "Text", "Kind", "Watermark", "PlaceholderText"])
        {
            if (element.Attribute(name) is { IsMarkupExtension: false } attribute && attribute.Value.Length > 0)
            {
                return name is "x:Name" or "Name" ? "#" + attribute.Value : $"\"{attribute.Value}\"";
            }
        }
        if (element.Text.Length > 0 && element.Children.Count == 0)
        {
            return $"\"{element.Text}\"";
        }
        return element.Attribute("Classes") is { } classes ? "." + string.Join('.', classes.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)) : null;
    }

    private Control ClassesSection(DemoSession session, XamlElement element, Control target)
    {
        var known = StyleClasses.For(target);
        _classChips = new WrapPanel { ItemSpacing = 4, LineSpacing = 4 };
        foreach (var name in known)
        {
            var chip = new ToggleButton { Classes = { "class-chip", "outline", "xsmall" }, Content = name, Tag = name };
            chip.IsCheckedChanged += (_, _) => ToggleClass(session, element, target, name, chip.IsChecked == true);
            _classChips.Children.Add(chip);
        }
        _classesText = new TextBox { Classes = { "small" }, PlaceholderText = "No classes", HorizontalAlignment = HorizontalAlignment.Stretch };
        _classesText.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                ApplyClassesText(session, element, target);
                e.Handled = true;
            }
        };
        _classesText.LostFocus += (_, _) => ApplyClassesText(session, element, target);
        var revert = new Button
        {
            Classes = { "ghost", "xsmall", "icon-only" },
            Content = new PathIcon { Data = Icons.Find("Undo2") },
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
            Tag = "revert-classes",
        };
        ToolTip.SetTip(revert, "Undo the change");
        revert.Click += (_, _) =>
        {
            session.RevertClasses(element, target);
            SyncClasses(target);
        };
        DockPanel.SetDock(revert, Avalonia.Controls.Dock.Right);
        var panel = new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Classes = { "inspector-label" }, Text = known.Count > 0 ? "CLASSES" : "CLASSES (THE THEME STYLES NONE)" },
            },
        };
        if (known.Count > 0)
        {
            panel.Children.Add(_classChips);
        }
        panel.Children.Add(new DockPanel { Children = { revert, _classesText } });
        SyncClasses(target);
        return new Border { Classes = { "inspector-section" }, Child = panel };
    }

    private void ToggleClass(DemoSession session, XamlElement element, Control target, string name, bool on)
    {
        if (_syncingClasses)
        {
            return;
        }
        var classes = DemoSession.ClassesOf(target).ToList();
        if (on)
        {
            // A size or a color replaces the one there was.
            classes.RemoveAll(c => StyleClasses.ExcludedBy(name).Contains(c));
            if (!classes.Contains(name))
            {
                classes.Add(name);
            }
        }
        else
        {
            classes.Remove(name);
        }
        session.SetClasses(element, target, classes);
        SyncClasses(target);
    }

    private void ApplyClassesText(DemoSession session, XamlElement element, Control target)
    {
        var classes = (_classesText?.Text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        if (!classes.SequenceEqual(DemoSession.ClassesOf(target)))
        {
            session.SetClasses(element, target, classes);
        }
        SyncClasses(target);
    }

    private void SyncClasses(Control target)
    {
        _syncingClasses = true;
        var classes = DemoSession.ClassesOf(target);
        if (_classChips is not null)
        {
            foreach (var chip in _classChips.Children.OfType<ToggleButton>())
            {
                chip.IsChecked = classes.Contains((string)chip.Tag!);
            }
        }
        if (_classesText is not null && !_classesText.IsFocused)
        {
            _classesText.Text = string.Join(' ', classes);
        }
        if (_classesText?.Parent is DockPanel row && row.Children.OfType<Button>().FirstOrDefault() is { } revert && Session is { } session)
        {
            revert.IsVisible = session.AreClassesEdited(target);
        }
        _syncingClasses = false;
    }

    private Control GroupSection(DemoSession session, XamlElement element, Control target, PropertyGroup group)
    {
        var rows = new StackPanel { Margin = new Thickness(4, 2, 10, 6) };
        var expander = Group(group.Title, $"{group.Title}  {group.Items.Count}", group.IsExpandedByDefault, rows);
        _groups.Add((expander, group, rows));
        void Build()
        {
            if (rows.Children.Count > 0)
            {
                return;
            }
            foreach (var item in group.Items)
            {
                var row = new PropertyRow(session, element, target, item);
                _rows.Add(row);
                rows.Children.Add(row);
            }
        }
        // Rows are built when the group first opens: a control has a hundred properties.
        if (expander.IsExpanded)
        {
            Build();
        }
        expander.PropertyChanged += (_, e) =>
        {
            if (e.Property == Expander.IsExpandedProperty && expander.IsExpanded)
            {
                Build();
            }
        };
        return expander;
    }

    private static Expander Group(string key, string title, bool expandedByDefault, Control content)
    {
        var chevron = new PathIcon { Data = Icons.Find("ChevronRight"), Width = 12, Height = 12, Margin = new Thickness(0, 0, 6, 0) };
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children = { chevron, new TextBlock { Classes = { "group-title" }, Text = title } },
        };
        var expander = new Expander
        {
            Header = header,
            Content = content,
            IsExpanded = Expanded.GetValueOrDefault(key, expandedByDefault),
            Padding = new Thickness(14, 0, 0, 0),
            Margin = new Thickness(0, 4),
        };
        if (Application.Current?.TryGetResource("UIKitCollapsible", null, out var theme) == true && theme is ControlTheme collapsible)
        {
            expander.Theme = collapsible;
        }
        chevron.RenderTransform = new RotateTransform(expander.IsExpanded ? 90 : 0);
        expander.PropertyChanged += (_, e) =>
        {
            if (e.Property == Expander.IsExpandedProperty)
            {
                Expanded[key] = expander.IsExpanded;
                chevron.RenderTransform = new RotateTransform(expander.IsExpanded ? 90 : 0);
            }
        };
        return expander;
    }

    private void ApplyFilter()
    {
        var query = _filter?.Text?.Trim() ?? "";
        foreach (var (expander, group, rows) in _groups)
        {
            if (query.Length == 0)
            {
                expander.IsVisible = true;
                foreach (var row in rows.Children)
                {
                    row.IsVisible = true;
                }
                continue;
            }
            var matches = group.Items.Any(i => i.AttributeName.Contains(query, StringComparison.OrdinalIgnoreCase));
            expander.IsVisible = matches;
            if (matches)
            {
                expander.IsExpanded = true;
            }
            foreach (var row in rows.Children.OfType<PropertyRow>())
            {
                row.IsVisible = row.Item.AttributeName.Contains(query, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private void OnTargetChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        foreach (var row in _rows)
        {
            if (row.Item.Property == e.Property)
            {
                row.Refresh();
            }
        }
    }

    private void OnTargetClassesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (!_syncingClasses && _target is { } target)
        {
            SyncClasses(target);
        }
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        foreach (var row in _rows)
        {
            row.Refresh();
        }
        if (_target is { } target)
        {
            SyncClasses(target);
        }
    }

    // Reset made a new demo: edit the same element of it.
    private void OnRecreated(object? sender, EventArgs e)
    {
        if (Session is { } session)
        {
            var element = Element is { } current && session.Map.ControlOf(current) is not null ? current : session.DefaultElement();
            Show(session, element);
        }
    }
}
