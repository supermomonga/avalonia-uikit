using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Inspector;

/// <summary>
/// A row of the property grid: the property's name (in the foreground when the XAML
/// sets it, bold with a dot when the grid changed it), its editor, and a button that
/// undoes the grid's change.
/// </summary>
public sealed class PropertyRow : Grid
{
    private readonly DemoSession _session;
    private readonly XamlElement _element;
    private readonly Control _target;
    private readonly TextBlock _name;
    private readonly Ellipse _dot;
    private readonly Button _revert;
    private readonly PropertyEditor _editor;

    public PropertyRow(DemoSession session, XamlElement element, Control target, PropertyItem item)
    {
        _session = session;
        _element = element;
        _target = target;
        Item = item;
        ColumnDefinitions = new ColumnDefinitions("10,140,*,28");
        MinHeight = 30;

        _dot = new Ellipse { Classes = { "edited-dot" }, IsVisible = false };
        _name = new TextBlock { Classes = { "property-name" }, Text = item.AttributeName };
        var type = Nullable.GetUnderlyingType(item.Property.PropertyType) ?? item.Property.PropertyType;
        var written = DemoSession.Written(element, item);
        ToolTip.SetTip(_name, written is null
            ? $"{item.AttributeName} ({type.Name})"
            : $"{item.AttributeName} ({type.Name})\nIn the XAML: {written}");
        _name.Classes.Set("written", DemoSession.IsWritten(element, item));

        _editor = PropertyEditor.Create(item.Property.PropertyType);
        _editor.Committed = value => _session.Set(_element, _target, Item, value);
        // A markup extension in the XAML (a binding, a resource) is replaced by the grid's value.
        if (element.Attribute(item.AttributeName) is { IsMarkupExtension: true } extension)
        {
            ToolTip.SetTip(_editor.View, $"The XAML sets {extension.Value}; a change replaces it.");
        }
        _editor.View.VerticalAlignment = VerticalAlignment.Center;
        _editor.View.Margin = new Thickness(0, 2);

        _revert = new Button
        {
            Classes = { "ghost", "xsmall", "icon-only" },
            Content = new PathIcon { Data = Icons.Find("Undo2") },
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(_revert, "Undo the change");
        _revert.Click += (_, _) =>
        {
            _session.Revert(_element, _target, Item);
            Refresh();
        };

        SetColumn(_name, 1);
        SetColumn(_editor.View, 2);
        SetColumn(_revert, 3);
        Children.Add(_dot);
        Children.Add(_name);
        Children.Add(_editor.View);
        Children.Add(_revert);
        Refresh();
    }

    /// <summary>The property.</summary>
    public PropertyItem Item { get; }

    /// <summary>Shows the property's current value and whether the grid changed it.</summary>
    public void Refresh()
    {
        if (!_editor.IsEditing)
        {
            _editor.Show(_target.GetValue(Item.Property));
        }
        var edited = _session.IsEdited(_target, Item);
        _name.Classes.Set("edited", edited);
        _dot.IsVisible = edited;
        _revert.IsVisible = edited;
    }
}
