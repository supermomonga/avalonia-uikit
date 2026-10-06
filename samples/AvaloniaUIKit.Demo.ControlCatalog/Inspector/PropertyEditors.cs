using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Inspector;

/// <summary>
/// The control that edits one property in the grid: it shows the property's value and
/// hands the value the user gives to <see cref="Committed"/>. <see cref="Show"/> does
/// not commit.
/// </summary>
public abstract class PropertyEditor
{
    private bool _showing;

    /// <summary>The editing control.</summary>
    public abstract Control View { get; }

    /// <summary>Raised with the value the user gives; returns whether the property took it.</summary>
    public Func<object?, bool>? Committed { get; set; }

    /// <summary>Whether the user is in the middle of an edit (a focused text box), which a refresh must not undo.</summary>
    public virtual bool IsEditing => false;

    /// <summary>Shows <paramref name="value"/>.</summary>
    public void Show(object? value)
    {
        _showing = true;
        try
        {
            Display(value);
        }
        finally
        {
            _showing = false;
        }
    }

    /// <summary>Puts <paramref name="value"/> in the control.</summary>
    protected abstract void Display(object? value);

    /// <summary>Hands a value the user gave to <see cref="Committed"/>, unless it came from <see cref="Show"/>.</summary>
    protected bool Commit(object? value) => !_showing && Committed?.Invoke(value) != false;

    /// <summary>The editor for values of <paramref name="type"/>.</summary>
    public static PropertyEditor Create(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        var nullable = underlying is not null || !type.IsValueType;
        var value = underlying ?? type;
        if (value == typeof(bool))
        {
            return new BoolEditor(nullable);
        }
        if (value.IsEnum)
        {
            return new ChoiceEditor(Enum.GetNames(value), nullable, name => Enum.Parse(value, name), v => v?.ToString(), null);
        }
        if (value == typeof(Geometry))
        {
            return new ChoiceEditor([.. Icons.Keys.Select(Icons.NameOf)], nullable, name => Icons.Find(name),
                v => v is Geometry g && Icons.KeyOf(g) is { } key ? Icons.NameOf(key) : null, IconRow);
        }
        if (value == typeof(Color) || value == typeof(IBrush))
        {
            return new ColorEditor(value == typeof(IBrush));
        }
        if (value == typeof(object))
        {
            return new TextEditor(typeof(string));
        }
        return new TextEditor(type);
    }

    private static Control IconRow(string name) => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Spacing = 8,
        Children =
        {
            new PathIcon { Data = Icons.Find(name), Width = 14, Height = 14 },
            new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center },
        },
    };
}

/// <summary>A check box for a bool, three-state for a nullable one.</summary>
internal sealed class BoolEditor : PropertyEditor
{
    private readonly CheckBox _box;

    public BoolEditor(bool nullable)
    {
        _box = new CheckBox { Classes = { "small" }, IsThreeState = nullable, VerticalAlignment = VerticalAlignment.Center };
        _box.IsCheckedChanged += (_, _) => Commit(nullable ? _box.IsChecked : _box.IsChecked == true);
    }

    public override Control View => _box;

    protected override void Display(object? value) => _box.IsChecked = value as bool?;
}

/// <summary>A choice among names: a drop-down list, with a search field when there are many.</summary>
internal sealed class ChoiceEditor : PropertyEditor
{
    private const string None = "(none)";
    private const int SearchFrom = 16;

    private readonly Func<string, object?> _parse;
    private readonly Func<object?, string?> _name;
    private readonly ComboBox? _combo;
    private readonly AvaloniaUIKit.Select? _select;

    public ChoiceEditor(IReadOnlyList<string> names, bool nullable, Func<string, object?> parse, Func<object?, string?> name, Func<string, Control>? row)
    {
        _parse = parse;
        _name = name;
        List<string> items = nullable ? [None, .. names] : [.. names];
        if (items.Count < SearchFrom && row is null)
        {
            _combo = new ComboBox { Classes = { "small" }, ItemsSource = items, HorizontalAlignment = HorizontalAlignment.Stretch, MaxDropDownHeight = 320 };
            _combo.SelectionChanged += (_, _) => Choose(_combo.SelectedItem as string);
            return;
        }
        _select = new AvaloniaUIKit.Select
        {
            Classes = { "small" },
            IsSearchable = true,
            ItemsSource = items,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxDropDownHeight = 320,
            SearchPlaceholderText = "Search",
        };
        if (row is not null)
        {
            _select.ItemTemplate = new FuncDataTemplate<string>((item, _) => item == None ? new TextBlock { Text = None } : row(item));
        }
        _select.SelectionChanged += (_, _) => Choose(_select.SelectedItem as string);
    }

    public override Control View => (Control?)_combo ?? _select!;

    private void Choose(string? item)
    {
        if (item is not null)
        {
            Commit(item == None ? null : _parse(item));
        }
    }

    protected override void Display(object? value)
    {
        var name = value is null ? None : _name(value);
        if (_combo is not null)
        {
            _combo.SelectedItem = name;
        }
        else
        {
            _select!.SelectedItem = name;
        }
    }
}

/// <summary>A color field with the theme's color picker (<c>uikit:ColorSelect</c>), for a color or a solid brush.</summary>
internal sealed class ColorEditor : PropertyEditor
{
    private readonly ColorSelect _select;
    private readonly TextBlock _other;
    private readonly Panel _panel;
    private readonly bool _brush;

    public ColorEditor(bool brush)
    {
        _brush = brush;
        _select = new ColorSelect { Classes = { "field", "small" }, HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "None" };
        _select.PropertyChanged += (_, e) =>
        {
            if (e.Property == ColorSelect.ColorProperty)
            {
                var color = _select.Color;
                Commit(color is null ? null : _brush ? new SolidColorBrush(color.Value).ToImmutable() : color.Value);
            }
        };
        // A gradient or another brush the picker cannot show: named, not edited.
        _other = new TextBlock { Classes = { "muted" }, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
        _panel = new Panel { Children = { _select, _other } };
    }

    public override Control View => _panel;

    protected override void Display(object? value)
    {
        var color = value switch
        {
            Color c => c,
            ISolidColorBrush brush => brush.Color,
            _ => (Color?)null,
        };
        var other = value is not null && color is null;
        _other.IsVisible = other;
        _select.IsVisible = !other;
        _other.Text = other ? value!.GetType().Name : "";
        _select.Color = color;
    }
}

/// <summary>
/// A text box for any value XAML writes as text: committed on Enter or when the focus
/// leaves, Escape restores the value. Up and Down step a number (Shift by 10).
/// </summary>
internal sealed class TextEditor : PropertyEditor
{
    private readonly TextBox _box;
    private readonly Type _type;
    private string _shown = "";

    public TextEditor(Type type)
    {
        _type = type;
        var numeric = XamlValues.IsNumber(Nullable.GetUnderlyingType(type) ?? type);
        _box = new TextBox { Classes = { "small" }, HorizontalAlignment = HorizontalAlignment.Stretch };
        if (numeric)
        {
            _box.PlaceholderText = "Auto";
        }
        _box.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        _box.LostFocus += (_, _) => Apply();
    }

    public override Control View => _box;

    public override bool IsEditing => _box.IsFocused && _box.Text != _shown;

    protected override void Display(object? value)
    {
        _shown = XamlValues.Display(value);
        _box.Text = _shown;
        DataValidationErrors.ClearErrors(_box);
    }

    private void Apply()
    {
        var text = _box.Text ?? "";
        if (text == _shown)
        {
            return;
        }
        if (XamlValues.TryParse(text, _type, out var value) && Commit(value))
        {
            _shown = text;
            DataValidationErrors.ClearErrors(_box);
        }
        else
        {
            DataValidationErrors.SetError(_box, new FormatException($"Not a {Nullable.GetUnderlyingType(_type)?.Name ?? _type.Name}"));
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                Apply();
                _box.SelectAll();
                e.Handled = true;
                break;
            case Key.Escape:
                _box.Text = _shown;
                DataValidationErrors.ClearErrors(_box);
                e.Handled = true;
                break;
            case Key.Up or Key.Down when XamlValues.IsNumber(Nullable.GetUnderlyingType(_type) ?? _type)
                && double.TryParse(_box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number):
                var step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
                _box.Text = XamlValues.Display(number + (e.Key == Key.Up ? step : -step));
                Apply();
                e.Handled = true;
                break;
        }
    }
}
