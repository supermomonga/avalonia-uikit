using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Badge (badge.rs): a count, a dot or an icon over the top right
/// corner of its content. A count of 0 shows nothing; past
/// <see cref="Maximum"/> it shows "Maximum+". An <see cref="Icon"/> takes
/// precedence over <see cref="IsDot"/>, which takes precedence over the count.
/// </summary>
public class Badge : ContentControl
{
    /// <summary>The number to show.</summary>
    public static readonly StyledProperty<int> CountProperty =
        AvaloniaProperty.Register<Badge, int>(nameof(Count));

    /// <summary>The largest number shown as is (GPUI's default is 99).</summary>
    public static readonly StyledProperty<int> MaximumProperty =
        AvaloniaProperty.Register<Badge, int>(nameof(Maximum), 99);

    /// <summary>Whether to show a dot instead of the count.</summary>
    public static readonly StyledProperty<bool> IsDotProperty =
        AvaloniaProperty.Register<Badge, bool>(nameof(IsDot));

    /// <summary>An icon to show in a circle at the bottom right instead of the count.</summary>
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<Badge, Geometry?>(nameof(Icon));

    /// <summary>The badge's color (GPUI's <c>color</c>); the theme's red by default.</summary>
    public static readonly StyledProperty<IBrush?> BadgeBackgroundProperty =
        AvaloniaProperty.Register<Badge, IBrush?>(nameof(BadgeBackground));

    /// <summary>The text the count shows.</summary>
    public static readonly DirectProperty<Badge, string> TextProperty =
        AvaloniaProperty.RegisterDirect<Badge, string>(nameof(Text), b => b.Text);

    private string _text = "0";

    static Badge()
    {
        CountProperty.Changed.AddClassHandler<Badge>((b, _) => b.Update());
        MaximumProperty.Changed.AddClassHandler<Badge>((b, _) => b.Update());
        IsDotProperty.Changed.AddClassHandler<Badge>((b, _) => b.Update());
        IconProperty.Changed.AddClassHandler<Badge>((b, _) => b.Update());
    }

    /// <summary>Creates a badge.</summary>
    public Badge() => Update();

    /// <inheritdoc cref="CountProperty"/>
    public int Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    /// <inheritdoc cref="MaximumProperty"/>
    public int Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <inheritdoc cref="IsDotProperty"/>
    public bool IsDot
    {
        get => GetValue(IsDotProperty);
        set => SetValue(IsDotProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="BadgeBackgroundProperty"/>
    public IBrush? BadgeBackground
    {
        get => GetValue(BadgeBackgroundProperty);
        set => SetValue(BadgeBackgroundProperty, value);
    }

    /// <inheritdoc cref="TextProperty"/>
    public string Text
    {
        get => _text;
        private set => SetAndRaise(TextProperty, ref _text, value);
    }

    private void Update()
    {
        var icon = Icon is not null;
        var dot = !icon && IsDot;
        PseudoClasses.Set(":icon", icon);
        PseudoClasses.Set(":dot", dot);
        PseudoClasses.Set(":count", !icon && !dot && Count > 0);
        Text = Count > Maximum
            ? Maximum.ToString(CultureInfo.InvariantCulture) + "+"
            : Count.ToString(CultureInfo.InvariantCulture);
    }
}
